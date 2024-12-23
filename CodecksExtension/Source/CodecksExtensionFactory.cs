namespace Xarbrough.CodecksPlasticIntegration;

using Codice.Client.IssueTracker;
using Newtonsoft.Json.Linq;

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
	private const string extensionName = "Codecks";

	public IssueTrackerConfiguration GetConfiguration(
		IssueTrackerConfiguration storedConfiguration)
	{
		var configValues = new ConfigValues(storedConfiguration);
		return configValues.BuildPlasticConfiguration();
	}

	public IPlasticIssueTrackerExtension GetIssueTrackerExtension(
		IssueTrackerConfiguration configuration)
	{
		string appSettingsJson = Resources.ReadAllText("AppSettings.json");
		bool advancedFiltersEnabled = (JObject.Parse(appSettingsJson)
				.GetValue("AdvancedFilters") ?? false)
			.Value<bool>();
		
		var configValues = new ConfigValues(configuration, advancedFiltersEnabled);

		IPlasticIssueTrackerExtension extension =
			new ExtensionErrorHandler(
					new CodecksExtension(extensionName, configValues));

		return extension;
	}

	public string GetIssueTrackerName() => extensionName;
}
