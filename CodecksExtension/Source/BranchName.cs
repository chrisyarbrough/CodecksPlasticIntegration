namespace Xarbrough.CodecksPlasticIntegration;

static class BranchName
{
	/// <summary>
	/// Given a branch name that includes nested branches, e.g. "/main/nem-123",
	/// outputs the task ID "123".
	/// </summary>
	public static bool TryExtractTaskFromFullName(
		string fullBranchName, string branchPrefix, out string taskId)
	{
		string shortBranchName = FullToShortName(fullBranchName);
		return TryExtractTaskFromShortName(shortBranchName, branchPrefix, out taskId);
	}

	/// <summary>
	/// Converts a full branch name including nested branches to the 'leave' branch.
	/// </summary>
	public static string FullToShortName(string fullBranchName)
	{
		if (fullBranchName == null)
			throw new ArgumentNullException(nameof(fullBranchName));

		int index = fullBranchName.LastIndexOf("/", StringComparison.Ordinal);
		if (index >= 0)
			return fullBranchName[(index + 1)..];

		return fullBranchName;
	}

	public static bool TryExtractTaskFromShortName(
		string shortBranchName, string branchPrefix, out string taskID)
	{
		branchPrefix ??= string.Empty;

		// If a branch prefix is set, branches not starting with it, are ignored.
		// But if the branch prefix is not set, it becomes impossible to predict
		// whether the branch name is a valid task ID or not. So simply try sending it.
		if (shortBranchName.StartsWith(branchPrefix) == false)
		{
			taskID = null;
			return false;
		}

		taskID = shortBranchName[branchPrefix.Length..];
		return true;
	}
}
