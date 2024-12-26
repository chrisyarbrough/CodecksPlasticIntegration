namespace Xarbrough.CodecksPlasticIntegration;

using Codice.Client.IssueTracker;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using ParamType = Codice.Client.IssueTracker.IssueTrackerConfigurationParameterType;

/// <summary>
/// Defines the available configuration parameters.
/// </summary>
/// <remarks>
/// This class is a facade for the <see cref="IssueTrackerConfiguration"/> provided by Plastic.
/// For filters, an empty string disables the filter.
/// </remarks>
sealed class Configuration
{
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

	[ParamInfo("E-Mail", ParamType.User)]
	public readonly Func<string> Email;

	[ParamInfo("Password", ParamType.Password)]
	public readonly Func<string> Password;

	[ParamInfo("Account Name", ParamType.Text)]
	public readonly Func<string> AccountName;

	[ParamInfo("Branch Prefix", ParamType.BranchPrefix)]
	public readonly Func<string> BranchPrefix;

	[ParamInfo("Project Filter", ParamType.Text, isAdvanced: true)]
	public readonly Func<string> ProjectFilter;

	[ParamInfo("Deck Filter", ParamType.Text, isAdvanced: true)]
	public readonly Func<string> DeckFilter;

#pragma warning restore CS0649

	private readonly IssueTrackerConfiguration config;
	private readonly bool enableAdvancedFilters;

	public Configuration(IssueTrackerConfiguration config)
	{
		this.config = config;

		string appSettingsJson = Resources.ReadAllText("AppSettings.json");
		enableAdvancedFilters = (JObject.Parse(appSettingsJson).GetValue("AdvancedFilters") ?? false).Value<bool>();

		foreach (var field in GetFields())
		{
			var info = field.GetCustomAttribute<ParamInfo>()!;
			field.SetValue(this, info.IsAdvanced ? GetAdvancedValue(info.Name) : () => config.GetValue(info.Name));
		}
	}

	private IEnumerable<FieldInfo> GetFields() => GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

	private Func<string> GetAdvancedValue(string key)
	{
		// If the advanced filters are disabled, return a value as if the user wanted to not set/use this parameter.
		return () => enableAdvancedFilters ? config.GetValue(key) : string.Empty;
	}

	/// <summary>
	/// Initializes the stored configuration managed by the Plastic client.
	/// </summary>
	/// <remarks>
	/// It is impossible to tell if the provided configuration contains
	/// values set by the user. The only thing known is whether
	/// the loaded values are defaults or something else.
	/// </remarks>
	public IssueTrackerConfiguration BuildPlasticConfiguration()
	{
		// Task per branch or per changeset.
		ExtensionWorkingMode workingMode = GetWorkingMode(config);

		// Settings that the user configures in the preferences window for the extension.
		var parameters = GetConfigurationParameters(config);

		return new IssueTrackerConfiguration(workingMode, parameters);
	}

	private static ExtensionWorkingMode GetWorkingMode(IssueTrackerConfiguration config)
	{
		if (config == null || config.WorkingMode == ExtensionWorkingMode.None)
			return ExtensionWorkingMode.TaskOnBranch;

		return config.WorkingMode;
	}

	private List<IssueTrackerConfigurationParameter> GetConfigurationParameters(IssueTrackerConfiguration config)
	{
		return CreateDefaultParameters()
			.Select(parameter =>
			{
				// Overwrite the default values with the stored configuration.
				if (TryGetValue(config, parameter.Name, out string storedValue))
					parameter.Value = storedValue.Trim();

				return parameter;
			}).ToList();
	}

	private static bool TryGetValue(IssueTrackerConfiguration config, string name, out string storedValue)
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
		return GetFields()
			.Select(field => field.GetCustomAttribute<ParamInfo>()!)
			.Where(info => !info.IsAdvanced || enableAdvancedFilters)
			.Select(info => new IssueTrackerConfigurationParameter
			{
				Name = info.Name,
				Value = string.Empty,
				Type = info.Type,
			});
	}

	[AttributeUsage(AttributeTargets.Field)]
	private class ParamInfo : Attribute
	{
		public readonly string Name;
		public readonly ParamType Type;
		public readonly bool IsAdvanced;

		public ParamInfo(string name, ParamType type, bool isAdvanced = false)
		{
			Name = name;
			Type = type;
			IsAdvanced = isAdvanced;
		}
	}
}