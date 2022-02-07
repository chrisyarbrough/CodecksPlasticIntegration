namespace Xarbrough.CodecksPlasticIntegration
{
	using Codice.Client.IssueTracker;
	using Codice.Utils;
	using Newtonsoft.Json.Linq;
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// The main interface implementation for the issue tracker extension.
	/// </summary>
	/// <remarks>
	///	Because the Codecks web API is not officially supported or documented,
	/// the implementation of some of the calls may be awkward. See the
	/// repository readme for more details.
	/// </remarks>
	public class CodecksExtension : IPlasticIssueTrackerExtension
	{
		internal const string NAME = "Codecks";

		internal const string API_BASE_URL = "API Base URL";
		internal const string BRANCH_PREFIX_KEY = "Branch Prefix";
		internal const string EMAIL = "E-Mail";
		internal const string PASSWORD = "Password";
		internal const string ACCOUNT_NAME = "Account Name";

		/// <summary>
		/// Settings configured by the user in Plastic. When the extension is first started
		/// this may contain invalid data. Once the data is correct, a new instance
		/// of this class will be created and passed a fresh copy of the config.
		/// </summary>
		private readonly IssueTrackerConfiguration config;
		
		/// <summary>
		/// A helper class to interface with the Codecks API.
		/// </summary>
		private readonly CodecksService service;
		
		/// <summary>
		/// Maps bidirectionally between user guid and email.
		/// </summary>
		private readonly UserLookup userLookup = new UserLookup();
		
		/// <summary>
		/// Converts the card 'accountSeq' value to a three-letter
		/// display label and vice-versa.
		/// </summary>
		private readonly CardIDConverter idConverter = new CardIDConverter();

		/// <summary>
		/// Maps between [Key: accountSeq] and [Value: guid].
		/// </summary>
		private readonly Dictionary<string, string> cardGuidLookup = new Dictionary<string, string>();

		internal CodecksExtension(IssueTrackerConfiguration config)
		{
			this.config = config;
			this.service = new CodecksService();
		}

		public string GetExtensionName() => NAME;

		/// <summary>
		/// A button in Preferences > Issue trackers.
		/// </summary>
		/// <remarks>
		/// The passed configuration argument is the updated version
		/// from the UI. If this test succeeds, the Plastic clients
		/// creates a new extension instance and calls <see cref="Connect"/>.
		/// </remarks>
		public bool TestConnection(IssueTrackerConfiguration configuration)
		{
			return TryConnect(configuration);
		}

		public void Connect()
		{
			TryConnect(this.config);
		}

		public void Disconnect()
		{
			// Nothing to cleanup, but the interface defines this method.
		}

		private bool TryConnect(IssueTrackerConfiguration config)
		{
			try
			{
				// This may fail and throw because of invalid credentials.
				// However, the exception doesn't contain useful information
				// since it's either Internal Server Error or Bad Request.
				service.Login(
					config.GetValue(API_BASE_URL),
					config.GetValue(ACCOUNT_NAME),
					config.GetValue(EMAIL),
					CryptoServices.GetDecryptedPassword(config.GetValue(PASSWORD))
				);

				// To make sure the connection actually works,
				// send a simple request with the logged-in user.
				return service.LoadAccountID().Length > 0;
			}
			catch (Exception)
			{
				// Another reason to swallow the exception here, is that the
				// connect call can be made by Plastic in situations where the user
				// doesn't expect a login attempt to happen. For example, when 
				// switching between different workspaces.
				return false;
			}
		}

		/// <summary>
		/// Called when creating a new branch from a task.
		/// Returns all tasks in the issue tracker system.
		/// </summary>
		public List<PlasticTask> GetPendingTasks()
		{
			const string query =
				"{\"query\":{\"_root\":[{\"account\":" +
				"[{\"cards({\\\"status\\\":\\\"not_started\\\",\\\"visibility\\\":\\\"default\\\"})\":" +
				"[\"title\",\"cardId\",\"content\",\"status\",\"assigneeId\",\"deckId\",\"accountSeq\"]}]}]}}";

			return FetchAndCacheCards(query);
		}

		/// <summary>
		/// Called when creating a new branch from a task.
		/// Returns tasks filtered by the email of the assigned user.
		/// </summary>
		public List<PlasticTask> GetPendingTasks(string assigneeEmail)
		{
			PopulateEmailUserLookup();
			string id = userLookup.EmailToID(assigneeEmail);

			string query =
				"{\"query\":{\"_root\":[{\"account\":[{\"cards({\\\"$and\\\":[{\\\"assigneeId\\\":[\\\"" +
				id +
				"\\\"]}],\\\"visibility\\\":\\\"default\\\",\\\"status\\\":\\\"not_started\\\"})\":" +
				"[\"title\",\"cardId\",\"content\",\"status\",\"assigneeId\",\"deckId\",\"accountSeq\"]}]}]}}";

			return FetchAndCacheCards(query);
		}

		private List<PlasticTask> FetchAndCacheCards(string query)
		{
			// TODO: This method is both command and query and will cause issues later.
			// For example, when we want to implement LoadTasks or GetTasksForBranches.
			// So far, we assume that cards will be loaded before the cache is used,
			// but this assumption will likely break. Maybe simply don't cache the lookups.
			IEnumerable<JProperty> cards = LoadCodecksCards(query);
			PopulateCardGuidLookup(cards);
			return BuildTasks(cards);
		}

		private void PopulateCardGuidLookup(IEnumerable<JProperty> cards)
		{
			foreach (JProperty property in cards)
			{
				string key = idConverter.IntToSeq((int)property.Value["accountSeq"]);
				string value = (string)property.Value["cardId"];
				cardGuidLookup[key] = value;
			}
		}

		/// <summary>
		/// Receives the data model sent by the Codecks API (a list of card objects)
		/// and converts each to a task representation for Plastic.
		/// </summary>
		private List<PlasticTask> BuildTasks(IEnumerable<JProperty> cards)
		{
			var tasks = new List<PlasticTask>();

			foreach (JProperty property in cards)
			{
				tasks.Add(new PlasticTask
				{
					Id = idConverter.IntToSeq((int)property.Value["accountSeq"]),
					Title = (string)property.Value["title"],
					Description = (string)property.Value["content"],
					Status = (string)property.Value["status"],
					Owner = userLookup.IDToEmail((string)property.Value["assigneeId"])
				});
			}

			return tasks;
		}

		private IEnumerable<JProperty> LoadCodecksCards(string query)
		{
			dynamic data = service.PostQuery(query);

			// The 'card' object contains multiple cards, but the Codecks API uses the singular name.
			foreach (JProperty property in data.card)
				yield return property;
		}

		private void PopulateEmailUserLookup()
		{
			userLookup.Clear();

			string getAllUsers =
				"{\"query\":{\"account(" +
				service.LoadAccountID() +
				")\":[{\"roles\":[{\"user\":[\"id\",\"name\",\"fullName\",{\"primaryEmail\":[\"email\"]}]}]}]}}";

			dynamic data = service.PostQuery(getAllUsers);
			foreach (JProperty property in data.userEmail)
			{
				string id = (string)property.Value["userId"];
				string mail = (string)property.Value["email"];
				userLookup.Add(id, mail);
			}
		}

		/// <summary>
		/// Called when selecting a branch in the Branches view and/or refreshing
		/// the tab in the "Extended Information" sidebar.
		/// </summary>
		public PlasticTask GetTaskForBranch(string fullBranchName)
		{
			// TODO: Implement GetTaskForBranch.
			// Full branch name example: "/main/cd-1rj"
			// The other official codecks extensions internally call a helper method
			// ExtensionServices.GetTaskIdFromFullBranchName.
			// However, the task ID in our case is the user-facing card ID label (e.g. 16r),
			// but again, what we need is either the accountSeq or guid.
			
			return new PlasticTask();
		}

		public void MarkTaskAsOpen(string taskId, string assignee)
		{
			// The taskID in this case is the short identifier e.g. "23h",
			// also called accountSeq in the API. However, the update API
			// requires the card GUID top be sent.

			string cardId = cardGuidLookup[taskId];

			string body = "{\"id\":\"" +
			              cardId +
			              "\",\"status\":\"started\"}";

			service.PostCardUpdate(body);
		}

		/// <summary>
		/// Called when clicking the "Open in browser" button in the
		/// extended information sidebar of the branches view.
		/// </summary>
		public void OpenTaskExternally(string taskId)
		{
			// TODO: Implement UpdateLinkedTasksToChangeset.
			const string browserURL = "";
			System.Diagnostics.Process.Start(browserURL);
		}

		public void UpdateLinkedTasksToChangeset(PlasticChangeset changeset, List<string> tasks)
		{
			// TODO: Implement UpdateLinkedTasksToChangeset.
		}
		
		public Dictionary<string, PlasticTask> GetTasksForBranches(List<string> fullBranchNames)
		{
			// TODO: Implement GetTasksForBranches.
			return new Dictionary<string, PlasticTask>();
		}

		public List<PlasticTask> LoadTasks(List<string> taskIds)
		{
			// TODO: Implement LoadTasks.
			return new List<PlasticTask>();
		}

		// Called when a changeset is created.
		public void LogCheckinResult(PlasticChangeset changeset, List<PlasticTask> tasks)
		{
			// TODO: Implement LogCheckinResult.
		}
	}
}
