namespace Xarbrough.CodecksPlasticIntegration;

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
static class QueryProvider
{
	private static readonly StringBuilder stringBuilder = new();
	private static readonly Dictionary<string, string> cache = new();

	public static string GetQuery(string fileName)
	{
		return ReadOrGetCachedFile($"Queries/{fileName}");
	}

	private static string ReadOrGetCachedFile(string name)
	{
		if (!cache.TryGetValue(name, out string content))
		{
			content = Resources.ReadAllText(name);
			content = content.Replace("'", "\\\"");
			content = Minimize(content);
			cache.Add(name, content);
		}

		return content;
	}

	private static string Minimize(string json)
	{
		stringBuilder.Clear();
		foreach (char c in json.Where(c => !char.IsSeparator(c) && !char.IsControl(c)))
		{
			stringBuilder.Append(c);
		}

		return stringBuilder.ToString();
	}
}