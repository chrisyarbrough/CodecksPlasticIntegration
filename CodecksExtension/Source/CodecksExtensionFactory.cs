namespace Xarbrough.CodecksPlasticIntegration
{
	using Codice.Client.IssueTracker;
	using System.Collections.Generic;

	/// <summary>
	/// Defines the available configuration parameters and
	/// creates the <see cref="CodecksExtension"/>.
	/// </summary>
	/// <remarks>
	/// The PlasticSCM client app instantiates this factory and uses it
	/// to display the available configuration options to the user and
	/// create an instance of the issue tracker interface implementation.
	/// </remarks>
	// ReSharper disable once UnusedType.Global
	public class CodecksExtensionFactory : IPlasticIssueTrackerExtensionFactory
	{
		private const string ENABLE_LOG = "Enable Log";

		public IssueTrackerConfiguration GetConfiguration(
			IssueTrackerConfiguration storedConfiguration)
		{
			// Task per branch or per changeset.
			ExtensionWorkingMode workingMode = GetWorkingMode(storedConfiguration);

			// Settings that the user configures in the preferences window for the extension.
			var parameters = GetConfigurationParameters(storedConfiguration);

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

		private static List<IssueTrackerConfigurationParameter> GetConfigurationParameters(
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

		private static IEnumerable<IssueTrackerConfigurationParameter> CreateDefaultParameters()
		{
			// Allows users to not use any branch prefix by not supplying a default value.
			// If the extension would define one here, it would be impossible to detect
			// whether the user wanted to set it to an empty string or
			// if this is the first time the config is loaded.
			yield return new IssueTrackerConfigurationParameter
			{
				Name = CodecksExtension.BRANCH_PREFIX_KEY,
				Value = string.Empty,
				Type = IssueTrackerConfigurationParameterType.BranchPrefix,
				IsGlobal = true
			};

			// Users currently don't have a reason to change the base url,
			// but in the future, Codecks might release a self-hosted version that 
			// is still backwards compatible in regards to individual endpoints.
			yield return new IssueTrackerConfigurationParameter
			{
				Name = CodecksExtension.API_BASE_URL,
				Value = "https://api.codecks.io/",
				Type = IssueTrackerConfigurationParameterType.Host,
				IsGlobal = true
			};

			yield return new IssueTrackerConfigurationParameter
			{
				Name = CodecksExtension.ACCOUNT_NAME,
				Value = string.Empty,
				Type = IssueTrackerConfigurationParameterType.Text,
				IsGlobal = false
			};

			yield return new IssueTrackerConfigurationParameter
			{
				Name = CodecksExtension.EMAIL,
				Value = string.Empty,
				Type = IssueTrackerConfigurationParameterType.User,
				IsGlobal = false
			};

			yield return new IssueTrackerConfigurationParameter
			{
				Name = CodecksExtension.PASSWORD,
				Value = string.Empty,
				Type = IssueTrackerConfigurationParameterType.Password,
				IsGlobal = false
			};

			yield return new IssueTrackerConfigurationParameter
			{
				Name = ENABLE_LOG,
				Value = "false",
				Type = IssueTrackerConfigurationParameterType.Boolean,
				IsGlobal = false
			};
		}

		public IPlasticIssueTrackerExtension GetIssueTrackerExtension(
			IssueTrackerConfiguration configuration)
		{
			IPlasticIssueTrackerExtension extension = new CodecksExtension(configuration);

			if (IsLoggingEnabled(configuration))
				extension = new LoggedIssueTrackerExtension(extension);

			return extension;
		}

		private static bool IsLoggingEnabled(IssueTrackerConfiguration configuration)
		{
			if (bool.TryParse(configuration.GetValue(ENABLE_LOG), out bool result))
				return result;

			return false;
		}

		public string GetIssueTrackerName() => CodecksExtension.NAME;
	}
}
