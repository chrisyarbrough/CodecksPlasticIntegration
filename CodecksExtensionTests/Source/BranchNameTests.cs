namespace Xarbrough.CodecksPlasticIntegration.Tests;

public class BranchNameTests
{
	[Test]
	public void FullToShortBranchName_NullArg_Throws()
	{
		Assert.Throws<ArgumentNullException>(() => { BranchName.FullToShortName(null); });
	}

	[Test]
	public void FullToShortBranchName_EmptyArg_ReturnsArg()
	{
		string shortBranchName = BranchName.FullToShortName(string.Empty);
		Assert.AreEqual(string.Empty, shortBranchName);
	}

	[TestCase("/main/nem-123", "nem-123")]
	[TestCase("/main/cd-th3", "cd-th3")]
	[TestCase("/main/abc", "abc")]
	public void FullToShortBranchName_ValidFullName_ReturnsShortName(
		string fullBranchName, string expectedShortName)
	{
		string shortBranchName = BranchName.FullToShortName(fullBranchName);
		Assert.AreEqual(expectedShortName, shortBranchName);
	}

	[TestCase("/main/nem-123/nem-5h7", "nem-5h7")]
	[TestCase("/main/cd-th3/nem-847", "nem-847")]
	[TestCase("/main/abc/efg", "efg")]
	public void FullToShortBranchName_ValidNestedFullName_ReturnsShortName(
		string fullBranchName, string expectedShortName)
	{
		string shortBranchName = BranchName.FullToShortName(fullBranchName);
		Assert.AreEqual(expectedShortName, shortBranchName);
	}

	[Test]
	public void TryExtractTaskIdFromBranchName_BranchPrefix_Mismatch()
	{
		// If a branch prefix is configured, branches without the prefix
		// should always fail the extraction test.
		bool success = BranchName.TryExtractTaskFromShortName(
			shortBranchName: "123",
			branchPrefix: "nem-",
			taskId: out string _);

		Assert.IsFalse(success);
	}

	[TestCase("")]
	[TestCase(null)]
	public void TryExtractTaskIdFromBranchName_NoBranchPrefix_Match(string branchPrefix)
	{
		// If no prefix is configured, the branch short name
		// is likely a valid task name. However, this doesn't mean
		// that the issue tracker API will recognize the id.
		// Usually, the configuration should return an empty string,
		// but we can't be sure that it's not also null at some point.
		const string shortBranchName = "123";

		bool success = BranchName.TryExtractTaskFromShortName(
			shortBranchName,
			branchPrefix,
			taskId: out string taskId);

		Assert.IsTrue(success);
		Assert.AreEqual(shortBranchName, taskId);
	}

	[TestCase("nem-123", "nem-", "123")]
	[TestCase("xd-eee", "xd-", "eee")]
	[TestCase("cod", "", "cod")]
	public void TryExtractTaskIdFromBranchName_ValidCases(
		string shortBranchName, string branchPrefix, string expectedTaskId)
	{
		bool success = BranchName.TryExtractTaskFromShortName(
			shortBranchName,
			branchPrefix,
			taskId: out string taskId);

		Assert.IsTrue(success);
		Assert.AreEqual(expectedTaskId, taskId);
	}
}
