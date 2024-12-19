namespace Xarbrough.CodecksPlasticIntegration;

using Codice.Client.IssueTracker;
using log4net;

/// <summary>
/// A decorator that adds informational logging to the interface methods
/// of a <see cref="IPlasticIssueTrackerExtension"/> instance and handles exceptions appropriately.
/// Some exceptions would cause annoying popups for the user, whereas others are relevant to show.
/// </summary>
class ExtensionErrorHandler : IPlasticIssueTrackerExtension
{
	private readonly IPlasticIssueTrackerExtension extension;
	private readonly ILog log;

	public ExtensionErrorHandler(IPlasticIssueTrackerExtension extension)
	{
		this.extension = extension;
		log = LogManager.GetLogger(GetExtensionName());
		log.Info("Extension logging initialized.");
	}

	public string GetExtensionName() => extension.GetExtensionName();

	public void Connect()
	{
		log.Info("Connect.");
		try
		{
			extension.Connect();
		}
		catch (Exception e)
		{
			log.Error(e);

			// A good reason to swallow the exception, is that the
			// connect call can be made in situations where the user
			// doesn't expect a login attempt to happen. For example,
			// when switching to a different workspace for which
			// credentials have not been set up yet.
		}
	}

	public void Disconnect()
	{
		log.Info("Disconnect.");
		extension.Disconnect();
	}

	public bool TestConnection(IssueTrackerConfiguration configuration)
	{
		log.Info("Test connection:\n" + configuration.ToLogString());
		try
		{
			// This may throw because of invalid credentials.
			return extension.TestConnection(configuration);
		}
		catch (Exception e)
		{
			log.Error(e);

			// Pass it on for Plastic to display in a popup.
			throw;
		}
	}

	public void LogCheckinResult(PlasticChangeset changeset, List<PlasticTask> tasks)
	{
		log.Info("Log checkin result for changeset:\n" + changeset.ToLogString());
		extension.LogCheckinResult(changeset, tasks);
	}

	public void UpdateLinkedTasksToChangeset(PlasticChangeset changeset, List<string> tasks)
	{
		log.Info("Update linked tasks to changeset: " + changeset.ToLogString());
		extension.UpdateLinkedTasksToChangeset(changeset, tasks);
	}

	public PlasticTask GetTaskForBranch(string fullBranchName)
	{
		log.Info("Get task for branch: " + fullBranchName);
		try
		{
			return extension.GetTaskForBranch(fullBranchName);
		}
		catch (Exception)
		{
			// This exception would open an annoying error popup every time
			// a user selects a branch.
			return null;
		}
	}

	public Dictionary<string, PlasticTask> GetTasksForBranches(List<string> fullBranchNames)
	{
		log.Info("Get tasks for branches:\n" + string.Join("\n", fullBranchNames));
		return extension.GetTasksForBranches(fullBranchNames);
	}

	public void OpenTaskExternally(string taskId)
	{
		log.Info("Open task externally: " + taskId);
		extension.OpenTaskExternally(taskId);
	}

	public List<PlasticTask> LoadTasks(List<string> taskIds)
	{
		log.Info("Load tasks:\n" + string.Join("\n", taskIds));
		try
		{
			return extension.LoadTasks(taskIds);
		}
		catch (Exception)
		{
			// The request failed, most likely because the provided issue ID
			// does not match any card in Codecks.
			return new List<PlasticTask>();
		}
	}

	public List<PlasticTask> GetPendingTasks()
	{
		log.Info("Get pending tasks.");
		return extension.GetPendingTasks();
	}

	public List<PlasticTask> GetPendingTasks(string assignee)
	{
		// The passed assignee is the Plastic user, but we don't want to confuse users, the implementation is using
		// the configured Codecks account from the extension settings.
		log.Info("Get pending tasks for assigned Codecks user.");
		return extension.GetPendingTasks(assignee);
	}

	public void MarkTaskAsOpen(string taskId, string assignee)
	{
		log.Info($"Mark task {taskId} of assignee {assignee} as open.");
		extension.MarkTaskAsOpen(taskId, assignee);
	}
}