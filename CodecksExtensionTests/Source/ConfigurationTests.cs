namespace Xarbrough.CodecksPlasticIntegration.Tests;

using Codice.Client.IssueTracker;

public class ConfigurationTests
{
	[Test]
	public void FieldsReturnValues()
	{
		var issueTrackerConfig = new IssueTrackerConfiguration();
		issueTrackerConfig.Parameters = new IssueTrackerConfigurationParameter[]
		{
			new("Branch Prefix", "xa", IssueTrackerConfigurationParameterType.Text, false),
		};
		var config = new Configuration(issueTrackerConfig);
		string prefix = config.BranchPrefix.Invoke();
		Assert.AreEqual("xa", prefix);
	}
}