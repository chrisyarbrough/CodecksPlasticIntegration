namespace Xarbrough.CodecksPlasticIntegration.Configuration;

using Codice.Client.IssueTracker;
using System.Collections.Generic;

/// <summary>
/// Defines the available configuration parameters.
/// </summary>
/// <remarks>
/// This class is a facade for the <see cref="IssueTrackerConfiguration"/>
/// provided by Plastic. For one part, it simple initializes the config
/// parameters that are presented to the user, but as a second benefit,
/// it also wraps some functionality and makes it easier to use for the
/// main extension class than using the IssueTrackerConfiguration directly.
/// </remarks>
sealed class ConfigValues
{
	public readonly ConfigValue<string> BranchPrefix;
	public readonly ConfigValue<string> Email;
	public readonly ConfigValue<string> Password;
	public readonly ConfigValue<string> AccountName;
	public readonly ConfigValue<bool> LogEnabled;

	private readonly IssueTrackerConfiguration configuration;

	public ConfigValues(IssueTrackerConfiguration configuration)
	{
		this.configuration = configuration;

		BranchPrefix = new StringConfigValue("Branch Prefix", configuration);
		Email = new StringConfigValue("E-Mail", configuration);
		Password = new StringConfigValue("Password", configuration);
		AccountName = new StringConfigValue("Account Name", configuration);
		LogEnabled = new BoolConfigValue("Log Enabled", configuration);
	}

	/// <summary>
	/// Initializes the stored configuration managed by the Plastic client.
	/// </summary>
	/// <remarks>
	/// It is impossible to tell if the provided configuration contains
	/// values set by the user. The only thing known is, whether
	/// the loaded values are defaults or something else.
	/// </remarks>
	public IssueTrackerConfiguration BuildPlasticConfiguration()
	{
		// Task per branch or per changeset.
		ExtensionWorkingMode workingMode = GetWorkingMode(configuration);

		// Settings that the user configures in the preferences window for the extension.
		var parameters = GetConfigurationParameters(configuration);

		return new IssueTrackerConfiguration(workingMode, parameters);
	}

	private static ExtensionWorkingMode GetWorkingMode(IssueTrackerConfiguration config)
	{
		if (config == null)
			return ExtensionWorkingMode.TaskOnBranch;

		if (config.WorkingMode == ExtensionWorkingMode.None)
			return ExtensionWorkingMode.TaskOnBranch;

		return config.WorkingMode;
	}

	private List<IssueTrackerConfigurationParameter> GetConfigurationParameters(
		IssueTrackerConfiguration config)
	{
		var parameters = new List<IssueTrackerConfigurationParameter>();
		foreach (var parameter in CreateDefaultParameters())
		{
			// Overwrite the default values with the stored configuration.
			if (TryGetValue(config, parameter.Name, out string storedValue))
				parameter.Value = storedValue.Trim();

			parameters.Add(parameter);
		}
		return parameters;
	}

	private static bool TryGetValue(
		IssueTrackerConfiguration config, string name, out string storedValue)
	{
		string configValue = config?.GetValue(name);
		if (!string.IsNullOrEmpty(configValue))
		{
			storedValue = configValue;
			return true;
		}
		storedValue = string.Empty;
		return false;
	}

	private IEnumerable<IssueTrackerConfigurationParameter> CreateDefaultParameters()
	{
		// Allow users to not use any branch prefix by not supplying a default value.
		// If the extension would define one here, it would be impossible to detect
		// whether the user wanted to set it to an empty string or
		// if this is the first time the config is loaded.
		yield return new IssueTrackerConfigurationParameter
		{
			Name = BranchPrefix.Key,
			Value = string.Empty,
			Type = IssueTrackerConfigurationParameterType.BranchPrefix,
			IsGlobal = true
		};

		yield return new IssueTrackerConfigurationParameter
		{
			Name = AccountName.Key,
			Value = string.Empty,
			Type = IssueTrackerConfigurationParameterType.Text,
			IsGlobal = false
		};

		yield return new IssueTrackerConfigurationParameter
		{
			Name = Email.Key,
			Value = string.Empty,
			Type = IssueTrackerConfigurationParameterType.User,
			IsGlobal = false
		};

		yield return new IssueTrackerConfigurationParameter
		{
			Name = Password.Key,
			Value = string.Empty,
			Type = IssueTrackerConfigurationParameterType.Password,
			IsGlobal = false
		};

		yield return new IssueTrackerConfigurationParameter
		{
			Name = LogEnabled.Key,
			Value = "false",
			Type = IssueTrackerConfigurationParameterType.Boolean,
			IsGlobal = false
		};
	}
}