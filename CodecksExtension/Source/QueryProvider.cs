namespace Xarbrough.CodecksPlasticIntegration;

using System.IO;
using System.Reflection;
using System.Text;

/// <summary>
/// Queries sent to the Codecks API are GraphQL-like json strings.
/// They allow a powerful syntax to access resources in a flexible
/// way, however, working with raw json strings in C# is rather awkward
/// because of the need for escaping quotes and the lack of layout.
/// It was deemed a better workflow to develop and maintain each query
/// in a separate json file which allows taking advantage of IDE tools
/// such as syntax checking, highlighting and auto-formatting.
/// </summary>
class QueryProvider
{
	private readonly StringBuilder stringBuilder = new StringBuilder();
	private readonly Dictionary<string, string> cache = new Dictionary<string, string>();

	public string GetQuery(string fileName)
	{
		if (!cache.TryGetValue(fileName, out string query))
		{
			query = LoadQuery(fileName);
			cache.Add(fileName, query);
		}
		return query;
	}

	private string LoadQuery(string fileName)
	{
		// Query files must be marked as "embedded resource" during the build.

		const string namespacePath = "CodecksExtension.Queries.";
		Stream stream = Assembly.GetExecutingAssembly()
			.GetManifestResourceStream(namespacePath + fileName);

		using var reader = new StreamReader(stream);
		string content = reader.ReadToEnd();
		return MinimizeJson(content);
	}

	private string MinimizeJson(string json)
	{
		stringBuilder.Clear();
		foreach (char c in json)
		{
			if (char.IsSeparator(c) || char.IsControl(c))
				continue;

			stringBuilder.Append(c);
		}
		return stringBuilder.ToString();
	}
}
