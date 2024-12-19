namespace Xarbrough.CodecksPlasticIntegration;

using Codice.Client.IssueTracker;
using log4net;

/// <summary>
/// A decorator that adds informational logging to the interface methods
/// of a <see cref="IPlasticIssueTrackerExtension"/> instance.
/// </summary>
class LoggedIssueTrackerExtension : IPlasticIssueTrackerExtension
{
	private readonly IPlasticIssueTrackerExtension extension;
	private readonly ILog log;

	public LoggedIssueTrackerExtension(IPlasticIssueTrackerExtension extension)
	{
		this.extension = extension;
		this.log = LogManager.GetLogger(GetExtensionName());
		this.log.Info("Extension logging initialized.");
	}

	public string GetExtensionName() => extension.GetExtensionName();

	public void Connect()
	{
		log.Info("Connect.");
		extension.Connect();
	}

	public void Disconnect()
	{
		log.Info("Disconnect.");
		extension.Disconnect();
	}

	public bool TestConnection(IssueTrackerConfiguration configuration)
	{
		log.Info("Test connection:\n" + configuration.ToLogString());
		return extension.TestConnection(configuration);
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
		return extension.GetTaskForBranch(fullBranchName);
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
		return extension.LoadTasks(taskIds);
	}

	public List<PlasticTask> GetPendingTasks()
	{
		log.Info("Get pending tasks.");
		return extension.GetPendingTasks();
	}

	public List<PlasticTask> GetPendingTasks(string assignee)
	{
		log.Info("Get pending tasks for assignee: " + assignee);
		return extension.GetPendingTasks(assignee);
	}

	public void MarkTaskAsOpen(string taskId, string assignee)
	{
		log.Info($"Mark task {taskId} of assignee {assignee} as open.");
		extension.MarkTaskAsOpen(taskId, assignee);
	}
}
