namespace Xarbrough.CodecksPlasticIntegration.Configuration;

using Codice.Client.IssueTracker;

abstract class ConfigValue<T>
{
	public readonly string Key;

	protected readonly IssueTrackerConfiguration configuration;

	protected ConfigValue(string key, IssueTrackerConfiguration configuration)
	{
		this.Key = key;
		this.configuration = configuration;
	}

	public abstract T GetValue();
}