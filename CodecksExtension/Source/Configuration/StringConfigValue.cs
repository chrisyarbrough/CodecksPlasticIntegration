namespace Xarbrough.CodecksPlasticIntegration.Configuration;

using Codice.Client.IssueTracker;

class StringConfigValue : ConfigValue<string>
{
	public StringConfigValue(string key, IssueTrackerConfiguration configuration) 
		: base(key, configuration) { }

	public override string GetValue()
	{
		return configuration.GetValue(Key);
	}
}