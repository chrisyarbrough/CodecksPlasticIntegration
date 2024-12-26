namespace Xarbrough.CodecksPlasticIntegration;

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
class CodecksExtensionFactory : IPlasticIssueTrackerExtensionFactory
{
	public IssueTrackerConfiguration GetConfiguration(
		IssueTrackerConfiguration storedConfiguration)
	{
		var config = new Configuration(storedConfiguration);
		return config.BuildPlasticConfiguration();
	}

	public IPlasticIssueTrackerExtension GetIssueTrackerExtension(
		IssueTrackerConfiguration configuration)
	{
		var configValues = new Configuration(configuration);

		IPlasticIssueTrackerExtension extension =
			new ExtensionErrorHandler(
				new CodecksExtension(configValues));

		return extension;
	}

	public string GetIssueTrackerName() => CodecksExtension.Name;
}