namespace Xarbrough.CodecksPlasticIntegration
{
	using Codice.Client.IssueTracker;
	using Codice.Utils;
	using Newtonsoft.Json.Linq;
	using System;
	using System.Collections.Generic;
	using System.Net;

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
		/// Converts the card 'accountSeq' value to a three-letter
		/// display label and vice-versa.
		/// </summary>
		private readonly CardIDConverter idConverter = new CardIDConverter();

		/// <summary>
		/// Maps between [Key: accountSeq] and [Value: guid].
		/// </summary>
		private readonly Dictionary<string, string> cardGuidLookup =
			new Dictionary<string, string>();

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
			// Exceptions thrown here will be displayed in the 'create branch'
			// window in Plastic as helpful error messages.

			const string subQuery =
				"{\\\"visibility\\\":\\\"default\\\"}";

			return FetchAndCacheCards(subQuery);
		}

		/// <summary>
		/// Called when creating a new branch from a task.
		/// Returns tasks filtered by the email of the assigned user.
		/// </summary>
		public List<PlasticTask> GetPendingTasks(string assigneeEmail)
		{
			string userId = service.FetchUserId(assigneeEmail);

			string subQuery = "{\\\"$and\\\":[{\\\"assigneeId\\\":[\\\"" +
			                  userId +
			                  "\\\"]}],\\\"visibility\\\":\\\"default\\\"}";

			return FetchAndCacheCards(subQuery);
		}

		private List<PlasticTask> FetchAndCacheCards(string cardSubQuery)
		{
			var lookup = PopulateEmailUserLookup();
			IEnumerable<JProperty> cards = service.LoadCards(cardSubQuery);
			PopulateCardGuidLookup(cards);
			return BuildTasks(cards, lookup);
		}

		private Dictionary<string, string> PopulateEmailUserLookup()
		{
			var idToMail = new Dictionary<string, string>();

			string getAllUsers =
				"{\"query\":{\"account(" +
				service.LoadAccountID() +
				")\":[{\"roles\":[{\"user\":[\"id\",\"name\",\"fullName\",{\"primaryEmail\":[\"email\"]}]}]}]}}";

			dynamic data = service.PostQuery(getAllUsers);
			foreach (JProperty property in data.userEmail)
			{
				string id = (string)property.Value["userId"];
				string mail = (string)property.Value["email"];
				idToMail.Add(id, mail);
			}

			return idToMail;
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
		private List<PlasticTask> BuildTasks(
			IEnumerable<JProperty> cards,
			Dictionary<string, string> idToEmail)
		{
			var tasks = new List<PlasticTask>();

			foreach (JProperty property in cards)
				tasks.Add(BuildTask(property, idToEmail));

			return tasks;
		}

		private PlasticTask BuildTask(JProperty property, Dictionary<string, string> idToEmail)
		{
			string assigneeId = (string)property.Value["assigneeId"];
			string email = assigneeId != null ? idToEmail[assigneeId] : string.Empty;

			return new PlasticTask
			{
				Id = idConverter.IntToSeq((int)property.Value["accountSeq"]),
				Title = (string)property.Value["title"],
				Description = (string)property.Value["content"],
				Status = (string)property.Value["status"],
				Owner = email
			};
		}

		/// <summary>
		/// Called when selecting a branch in the Branches view and/or refreshing
		/// the tab in the "Extended Information" sidebar.
		/// </summary>
		public PlasticTask GetTaskForBranch(string fullBranchName)
		{
			// Full branch name example: "/main/cd-1rj"
			string branchPrefix = config.GetValue(BRANCH_PREFIX_KEY);

			if (BranchName.TryExtractTaskFromFullName(
				fullBranchName, branchPrefix, out string taskId))
			{
				try
				{
					// The parsed task ID is not validated at this point, but
					// the only way to do so is to try a request with it.
					return FetchTaskFromId(taskId);
				}
				catch (Exception)
				{
					// This exception would open an annoying error popup every time
					// a user selects a branch.
					return null;
				}
			}
			else
			{
				return null;
			}
		}

		private PlasticTask FetchTaskFromId(string taskId)
		{
			// This will also throw if the taskID is not convertible.
			// E.g. 'main' doesn't work because 'm' is not part of the converter letters.
			// But the test is still not enough to be 100% sure the web request will succeed.
			int accountSeq = idConverter.SeqToInt(taskId);

			string query =
				"{\"query\":{\"_root\":[{\"account\":" +
				"[{\"cards({\\\"accountSeq\\\":" + accountSeq +
				",\\\"visibility\\\":\\\"default\\\"})\":" +
				"[\"title\",\"cardId\",\"content\",\"status\",\"assigneeId\",\"accountSeq\"]}]}]}}";

			dynamic data = service.PostQuery(query);

			var idToEmail = PopulateEmailUserLookup();

			// Return the first result, since the card collection should only contain a single one.
			foreach (JProperty property in data.card)
			{
				return BuildTask(property, idToEmail);
			}
			return null;
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
			string browserURL =
				"https://" + config.GetValue(ACCOUNT_NAME) + ".codecks.io/card/" + taskId;
			System.Diagnostics.Process.Start(browserURL);
		}

		public Dictionary<string, PlasticTask> GetTasksForBranches(List<string> fullBranchNames)
		{
			var result = new Dictionary<string, PlasticTask>(fullBranchNames.Count);

			foreach (string fullBranchName in fullBranchNames)
				result.Add(fullBranchName, GetTaskForBranch(fullBranchName));

			return result;
		}

		public List<PlasticTask> LoadTasks(List<string> taskIds)
		{
			var tasks = new List<PlasticTask>();

			if (taskIds.Count == 0)
				return tasks;

			// TODO: Make this a TryParse-like method.
			try
			{
				for (int i = 0; i < taskIds.Count; i++)
				{
					taskIds[i] = idConverter.SeqToInt(taskIds[i]).ToString();
				}
			}
			catch (Exception)
			{
				// The provided task ID didn't event pass the conversion step,
				// so don't even try to send it to the API.
				return tasks;
			}

			string subQuery = "{\\\"accountSeq\\\":[" + string.Join(",", taskIds) + "]}";

			var idToEmail = PopulateEmailUserLookup();

			try
			{
				foreach (var card in service.LoadCards(subQuery))
				{
					tasks.Add(BuildTask(card, idToEmail));
				}
				return tasks;
			}
			catch (WebException)
			{
				// The request failed, most likely because the provided issue ID
				// does not match any card in Codecks.
				return tasks;
			}
		}

		/// <summary>
		/// Called when a changeset is created and reports to the issue tracker.
		/// </summary>
		public void LogCheckinResult(PlasticChangeset changeset, List<PlasticTask> tasks)
		{
			// It seems possible to implement different scenarios here, each of which
			// would require some additional user settings:
			// - Add a comment to the card simply stating that a checkin was performed.
			// - Include meta info (changeset comment, time, etc).
			// - Task on changeset mode: Set the card status to e.g. review or done.
			// - Task on branch mode: Add status comments or detect merge to main.
			// The last idea appears difficult to implement. We would have to define
			// what kind of merge means "feature done", since it could be a more
			// involved merge-pipeline etc. Probably best to only support changeset mode.
		}

		public void UpdateLinkedTasksToChangeset(PlasticChangeset changeset, List<string> tasks)
		{
			// TODO: Implement UpdateLinkedTasksToChangeset.
		}
	}
}
