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
	public const string Name = "Codecks";

	/// <summary>
	/// Settings configured by the user in Plastic. When the extension is first started
	/// this may contain invalid data. Once the data is correct, a new instance
	/// of this class will be created and passed a fresh copy of the config.
	/// </summary>
	private readonly Configuration config;

	/// <summary>
	/// Converts the card 'accountSeq' value to a three-letter
	/// display label and vice versa.
	/// </summary>
	private readonly CardIdConverter idConverter = new();

	/// <summary>
	/// A helper class to interface with the Codecks API.
	/// </summary>
	private CodecksService service;

	public CodecksExtension(Configuration config)
	{
		ArgumentNullException.ThrowIfNull(config);
		this.config = config;
	}

	public string GetExtensionName() => Name;

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
		service = BuildService(new Configuration(configuration));
		service.Login();
		return service.GetPendingCards(new Query()) != null;
	}

	/// <summary>
	/// Called at various times by the Plastic client to ensure
	/// the extension is connected to any web service.
	/// </summary>
	public void Connect()
	{
		service = BuildService(config);
		service.Login();
	}

	private static CodecksService BuildService(Configuration config)
	{
		var credentials = new CodecksCredentials(
			config.AccountName(),
			config.Email(),
			CryptoServices.GetDecryptedPassword(config.Password()));

		return new CodecksService(credentials);
	}

	public void Disconnect() => service?.Dispose();

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
		return GetTasks(assigneeEmail: null);
	}

	/// <summary>
	/// Called when creating a new branch from a task.
	/// Returns tasks filtered by the email of the assigned user.
	/// </summary>
	public List<PlasticTask> GetPendingTasks(string _)
	{
		// The passed assigneeEmail here is the one used to sign in to Unity/PlasticSCM.
		// If the mail used in Codecks is different, the lookup/filtering fails.
		// Therefore, we use the email from the configuration instead.
		return GetTasks(config.Email());
	}

	private List<PlasticTask> GetTasks(string assigneeEmail)
	{
		Query query = new Query
		{
			ProjectName = config.ProjectFilter(),
			DeckTitle = config.DeckFilter(),
			AssigneeEmail = assigneeEmail
		};

		IEnumerable<Card> cards = service.GetPendingCards(query);
		return ConvertToTasks(cards);
	}

	private List<PlasticTask> ConvertToTasks(IEnumerable<Card> cards)
		=> cards.Select(ConvertToTask).ToList();

	private PlasticTask ConvertToTask(Card card)
	{
		return new PlasticTask
		{
			Id = idConverter.IntToSeq(card.AccountSeq),
			Title = card.Title,
			Description = card.Content,
			Status = card.Status,
			Owner = card.Assignee,
		};
	}

	/// <summary>
	/// Called when selecting a branch in the Branches view and/or refreshing
	/// the tab in the "Extended Information" sidebar.
	/// </summary>
	public PlasticTask GetTaskForBranch(string fullBranchName)
	{
		// Full branch name example: "/main/cd-1rj"
		string branchPrefix = config.BranchPrefix();

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
		return fullBranchNames.Select(branchName => (Branch: branchName, Task: GetTaskForBranch(branchName)))
			.ToDictionary(t => t.Branch, t => t.Task);
	}

	private PlasticTask FetchTaskFromId(string taskId)
	{
		int accountSeq = idConverter.SeqToInt(taskId);
		Card card = service.GetCard(accountSeq);
		return ConvertToTask(card);
	}

	public void MarkTaskAsOpen(string taskId, string assignee)
	{
		// Example: taskId 1w5 accountSeq 7 cardId dc04f44d-be21-11ef-bae2-c71a6ad339aa
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
		string browserUrl = GetCardBrowserUrl(config.AccountName(), taskId);

		Process.Start(new ProcessStartInfo
		{
			FileName = browserUrl,
			UseShellExecute = true
		});
	}

	private static string GetCardBrowserUrl(string account, string idLabel)
	{
		// There are several ways to display a card in the web app:
		// Within the deck:
		// https://mysubdomain.codecks.io/decks/105-preproduction/card/1w4-start-documentation

		// Or as a single card on the hand:
		// https://mysubdomain.codecks.io/card/1w4-start-documentation

		// Conveniently, a short URL is also supported:
		// https://mysubdomain.codecks.io/card/1w4

		return $"https://{account}.codecks.io/card/{idLabel}";
	}

	/// <summary>
	/// Called in the task-on-changeset mode.
	/// </summary>
	public List<PlasticTask> LoadTasks(List<string> taskIds)
	{
		if (taskIds.Count == 0)
			return new List<PlasticTask>();

		Connect();

		var convertedCardIds = taskIds.Select(t => idConverter.SeqToInt(t).ToString());
		IEnumerable<Card> cards = service.GetCards(convertedCardIds);
		return ConvertToTasks(cards);
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