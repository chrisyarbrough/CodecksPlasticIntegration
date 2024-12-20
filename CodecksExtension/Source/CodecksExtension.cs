namespace Xarbrough.CodecksPlasticIntegration;

using Codice.Client.IssueTracker;
using Codice.Utils;
using System.Diagnostics;

/// <summary>
/// The main interface implementation for the issue tracker extension.
/// </summary>
/// <remarks>
///	This is the core extension class dealing with the logic how
/// to convert certain user interactions into Plastic tasks.
/// To make the implementation simple, exceptions are thrown liberally.
/// A separate class <seealso cref="ExtensionErrorHandler"/> deals
/// with decisions about how and if errors should be reported to the user.
/// </remarks>
class CodecksExtension : IPlasticIssueTrackerExtension
{
	private readonly string name;

	/// <summary>
	/// Settings configured by the user in Plastic. When the extension is first started
	/// this may contain invalid data. Once the data is correct, a new instance
	/// of this class will be created and passed a fresh copy of the config.
	/// </summary>
	private readonly ConfigValues configValues;

	/// <summary>
	/// Converts the card 'accountSeq' value to a three-letter
	/// display label and vice versa.
	/// </summary>
	private readonly CardIdConverter idConverter = new CardIdConverter();

	/// <summary>
	/// A helper class to interface with the Codecks API.
	/// </summary>
	private CodecksService service;

	internal CodecksExtension(string name, ConfigValues configValues)
	{
		if (string.IsNullOrEmpty(name))
			throw new ArgumentException(name);

		ArgumentNullException.ThrowIfNull(configValues);

		this.name = name;
		this.configValues = configValues;
	}

	public string GetExtensionName() => name;

	private static CodecksService BuildService(ConfigValues configValues)
	{
		var credentials = new CodecksCredentials(
			configValues.AccountName.GetValue(),
			configValues.Email.GetValue(),
			CryptoServices.GetDecryptedPassword(configValues.Password.GetValue()));

		return new CodecksService(credentials);
	}

	/// <summary>
	/// A button in Preferences > Issue trackers.
	/// </summary>
	/// <remarks>
	/// The passed configuration argument is the updated version
	/// from the UI. If this test succeeds, the Plastic client
	/// creates a new extension instance and calls <see cref="Connect"/>.
	/// </remarks>
	public bool TestConnection(IssueTrackerConfiguration configuration)
	{
		service = BuildService(new ConfigValues(configuration));
		service.Login();

		// To make sure the connection actually works,
		// send a simple request with the logged-in user.
		return service.GetAccountId().Length > 0;
	}

	/// <summary>
	/// Called at various times by the Plastic client to ensure
	/// the extension is connected to any web service.
	/// </summary>
	public void Connect()
	{
		service = BuildService(configValues);
		service.Login();
	}

	public void Disconnect()
	{
		service.Dispose();
	}

	/// <summary>
	/// Called when creating a new branch from a task.
	/// Returns all tasks in the issue tracker system.
	/// </summary>
	/// <remarks>
	///	Exceptions thrown here will be displayed in the 'create branch'
	/// window in Plastic as helpful error messages.
	/// </remarks>
	public List<PlasticTask> GetPendingTasks()
	{
		string deckTitle = configValues.DeckFilter.GetValue();
		string deckId = null;
		if (!string.IsNullOrEmpty(deckTitle))
			deckId = service.GetDeck(deckTitle).id;
		IEnumerable<Card> cards = service.GetPendingCards(deckId: deckId);
		return Convert(cards);
	}

	/// <summary>
	/// Called when creating a new branch from a task.
	/// Returns tasks filtered by the email of the assigned user.
	/// </summary>
	public List<PlasticTask> GetPendingTasks(string assigneeEmail)
	{
		// The passed assigneeEmail here is the one used to sign in to Unity/PlasticSCM.
		// If the mail used in Codecks is different, the lookup/filtering fails.
		// Therefore, we use the email from the configuration instead.
		string accountId = service.GetAccountId();
		string userId = service.FindUserIdByMail(accountId, configValues.Email.GetValue());

		string deckTitle = configValues.DeckFilter.GetValue();
		string deckId = null;
		if (!string.IsNullOrEmpty(deckTitle))
			deckId = service.GetDeck(deckTitle).id;

		IEnumerable<Card> cards = service.GetPendingCards(userId: userId, deckId: deckId);
		return Convert(cards);
	}

	private List<PlasticTask> Convert(IEnumerable<Card> cards)
	{
		var tasks = new List<PlasticTask>();

		var lookup = PopulateEmailUserLookup();
		string IdToEmail(string id) => lookup[id];

		foreach (Card card in cards)
			tasks.Add(Convert(card, IdToEmail));
		return tasks;
	}

	private PlasticTask Convert(Card card, Func<string, string> userIdToEmailConverter)
	{
		string owner = string.Empty;

		if (!string.IsNullOrEmpty(card.Assignee))
			owner = userIdToEmailConverter.Invoke(card.Assignee);

		return new PlasticTask
		{
			Title = card.Title,
			Description = card.Content,
			Id = idConverter.IntToSeq(card.AccountSeq),
			Status = card.Status,
			Owner = owner
		};
	}

	private Dictionary<string, string> PopulateEmailUserLookup()
	{
		var idToMail = new Dictionary<string, string>();

		string accountId = service.GetAccountId();
		IEnumerable<User> users = service.GetAllUsers(accountId);

		foreach (User user in users)
			idToMail.Add(user.id, user.email);

		return idToMail;
	}

	/// <summary>
	/// Called when selecting a branch in the Branches view and/or refreshing
	/// the tab in the "Extended Information" sidebar.
	/// </summary>
	public PlasticTask GetTaskForBranch(string fullBranchName)
	{
		// Full branch name example: "/main/cd-1rj"
		string branchPrefix = configValues.BranchPrefix.GetValue();

		if (BranchName.TryExtractTaskFromFullName(
			    fullBranchName, branchPrefix, out string taskId))
		{
			// A parsed task ID only means that the extension is able
			// to find the branch naming pattern. The only way to validate
			// a task ID is to attempt any request with it.
			return FetchTaskFromId(taskId);
		}
		else
		{
			// Unable to relate the name to any existing tasks.
			return null;
		}
	}

	public Dictionary<string, PlasticTask> GetTasksForBranches(List<string> fullBranchNames)
	{
		var result = new Dictionary<string, PlasticTask>(fullBranchNames.Count);

		foreach (string fullBranchName in fullBranchNames)
			result.Add(fullBranchName, GetTaskForBranch(fullBranchName));

		return result;
	}

	private PlasticTask FetchTaskFromId(string taskId)
	{
		int accountSeq = idConverter.SeqToInt(taskId);
		Card card = service.GetCard(accountSeq);
		string email = service.GetUserEmail(card.Assignee);
		return Convert(card, _ => email);
	}

	public void MarkTaskAsOpen(string taskId, string assignee)
	{
		// Task id example: 1w5 (display label on the Codecks card).
		int accountSeq = idConverter.SeqToInt(taskId);
		Card card = service.GetCard(accountSeq);
		service.SetCardStatusToStarted(card.CardId);
	}

	/// <summary>
	/// Called when clicking the "Open in browser" button in the
	/// extended information sidebar of the branches view.
	/// </summary>
	public void OpenTaskExternally(string taskId)
	{
		string browserURL = CodecksService.GetCardBrowserURL(
			configValues.AccountName.GetValue(),
			taskId);

		Process.Start(new ProcessStartInfo
		{
			FileName = browserURL,
			UseShellExecute = true
		});
	}

	/// <summary>
	/// Called in the task-on-changeset mode.
	/// </summary>
	public List<PlasticTask> LoadTasks(List<string> taskIds)
	{
		if (taskIds.Count == 0)
			return new List<PlasticTask>();

		Connect();

		var convertedCardIds = new List<string>();

		for (int i = 0; i < taskIds.Count; i++)
		{
			string id = idConverter.SeqToInt(taskIds[i]).ToString();
			convertedCardIds.Add(id);
		}

		var cards = service.GetCards(convertedCardIds);
		return Convert(cards);
	}

	/// <summary>
	/// Called when a changeset is created and reports to the issue tracker.
	/// </summary>
	public void LogCheckinResult(PlasticChangeset changeset, List<PlasticTask> tasks)
	{
		// It is possible to implement different scenarios here, each of which
		// would require some additional user settings:
		// - Add a comment to the card simply stating that a check-in was performed.
		// - Include meta info (changeset comment, time, etc.).
		// - Task on changeset mode: Set the card status to e.g. review or done.
		// - Task on branch mode: Add status comments or detect merge to main.
		// The last idea appears difficult to implement. We would have to define
		// what kind of merge means "feature done", since it could be a more
		// involved merge-pipeline etc. Probably best to only support changeset mode.
	}

	/// <summary>
	/// Called in the task-on-changeset mode when the user modifies the linked tasks.
	/// </summary>
	public void UpdateLinkedTasksToChangeset(PlasticChangeset changeset, List<string> tasks)
	{
		// This is currently not showing in the Plastic UI, so it is not implemented.
	}
}