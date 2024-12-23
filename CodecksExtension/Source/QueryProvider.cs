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
class QueryProvider
{
	private readonly StringBuilder stringBuilder = new();
	private readonly Dictionary<string, string> cache = new();

	public string GetQuery(string fileName)
	{
		if (!cache.TryGetValue(fileName, out string query))
		{
			query = LoadQuery(fileName);
			cache.Add(fileName, query);
		}
		return query;
	}

	public string GetFilter(string fileName)
	{
		string filter = Resources.ReadAllText($"/Queries/Filters/{fileName}");
		filter = filter.Replace("'", "\\\"");
		return Minimize(filter);
	}

	private string LoadQuery(string fileName)
	{
		string content = Resources.ReadAllText($"/Queries/{fileName}");
		return Minimize(content);
	}

	public string Minimize(string json)
	{
		stringBuilder.Clear();
		foreach (char c in json.Where(c => !char.IsSeparator(c) && !char.IsControl(c)))
		{
			stringBuilder.Append(c);
		}
		return stringBuilder.ToString();
	}
}