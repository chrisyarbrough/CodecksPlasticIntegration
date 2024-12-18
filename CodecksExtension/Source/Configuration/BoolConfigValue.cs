namespace Xarbrough.CodecksPlasticIntegration.Configuration;

using Codice.Client.IssueTracker;

class BoolConfigValue : ConfigValue<bool>
{
	public BoolConfigValue(string key, IssueTrackerConfiguration configuration) 
		: base(key, configuration) { }

	public override bool GetValue()
	{
		string stringValue = configuration.GetValue(Key);
		if (bool.TryParse(stringValue, out bool boolValue))
			return boolValue;
		return false;
	}
}