namespace Xarbrough.CodecksPlasticIntegration;

using Codice.Client.IssueTracker;
using System.Reflection;
using System.Text;

/// <summary>
/// Adds string formatting to Plastic data types to facilitate logging.
/// </summary>
static class LogStringExtensions
{
	public static string ToLogString(this IssueTrackerConfiguration configuration)
	{
		var sb = new StringBuilder();
		sb.Append("Working Mode: ").Append(configuration.WorkingMode).AppendLine();
		sb.Append("Parameters:\n");
		foreach (IssueTrackerConfigurationParameter parameter in configuration.GetAllParameters()
			         .Where(parameter => parameter.Type != IssueTrackerConfigurationParameterType.Password))
		{
			AppendPublicFields(sb, parameter);
			sb.AppendLine();
		}
		return sb.ToString();
	}

	public static string ToLogString(this PlasticChangeset changeset)
	{
		var sb = new StringBuilder();

		// It's ok to only return the most important simple fields.
		// The 'Items' or 'Children' members won't likely add any useful information.
		AppendPublicFields(sb, changeset);

		return sb.ToString();
	}

	/// <summary>
	/// Formats the public fields if they are simple data types.
	/// Complex nested objects or collection types produce undefined results.
	/// </summary>
	private static void AppendPublicFields(StringBuilder sb, object obj)
	{
		FieldInfo[] fields = obj.GetType().GetFields(
			BindingFlags.Instance |
			BindingFlags.Public |
			BindingFlags.DeclaredOnly);

		foreach (FieldInfo field in fields)
		{
			sb.Append(field.Name).Append('=');
			sb.Append(field.GetValue(obj)).AppendLine();
		}
	}
}
