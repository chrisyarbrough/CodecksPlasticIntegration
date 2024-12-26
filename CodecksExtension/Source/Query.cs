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
sealed class Query
{
	public string ProjectName { get; init; }
	public string DeckTitle { get; init; }
	public string AssigneeEmail { get; init; }

	public string Build()
	{
		string query = Load("GetPendingCards.json");

		(string, string, string, string)[] replacements =
		{
			("ProjectFilter", "Project", ProjectName, $$"""({\"name\":\"{{ProjectName}}\"})"""),
			("DeckFilter", "Deck", DeckTitle, $$"""({\"title\":\"{{DeckTitle}}\"})"""),
			("AssigneeFilter", "AssigneeEmail", AssigneeEmail,
				$$$$""",{\"assignee\":{\"primaryEmail\":{\"email\":\"{{{{AssigneeEmail}}}}\"}}}"""),
		};

		foreach (var (filterName, valueName, value, filterValue) in replacements)
		{
			query = query.Replace($"<{filterName}>",
				string.IsNullOrWhiteSpace(value) ? "" : filterValue.Replace($"<{valueName}>", value));
		}

		return query;
	}

	public static string Load(string fileName)
	{
		return Minimize(
			Resources.ReadAllText($"Queries/{fileName}")
				.Replace("'", "\\\""));
	}

	private static string Minimize(string json)
	{
		StringBuilder stringBuilder = new();
		foreach (char c in json.Where(c => !char.IsSeparator(c) && !char.IsControl(c)))
		{
			stringBuilder.Append(c);
		}

		return stringBuilder.ToString();
	}
}