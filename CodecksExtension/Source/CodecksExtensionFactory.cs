namespace Xarbrough.CodecksPlasticIntegration;

using Configuration;
using Codice.Client.IssueTracker;

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
		var configValues = new ConfigValues(configuration);

		IPlasticIssueTrackerExtension extension =
			new ExtensionErrorHandling(
				new CodecksExtension(extensionName, configValues));

		// TODO: Not sure what's going on, but sometimes this seems to return false, although it should be true.
		// if (configValues.LogEnabled.GetValue())
			extension = new LoggedIssueTrackerExtension(extension);

		return extension;
	}

	public string GetIssueTrackerName() => extensionName;
}
