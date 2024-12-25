namespace Xarbrough.CodecksPlasticIntegration;

public sealed class Query
{
	public string ProjectName { get; init; }
	public string DeckTitle { get; init; }
	public string AssigneeEmail { get; init; }

	public string Build()
	{
		string query =
			"""
			{
			  "query": {
			    "_root": [
			      {
			        "account": [
			          {
			            "projects<ProjectFilter>": [
			              {
			                "decks<DeckFilter>": [
			                  {
			                    "cards({\"$and\":[{\"visibility\":\"default\"},{\"status\":{\"op\":\"neq\",\"value\":\"done\"}},{\"assignee\":{<AssigneeFilter>}}]})": [
			                      "accountSeq",
			                      "cardId",
			                      "title",
			                      "status",
			                      "assignee",
			                      "content"
			                    ]
			                  }
			                ]
			              }
			            ]
			          }
			        ]
			      }
			    ]
			  }
			}
			""";

		(string, string, string, string)[] replacements =
		{
			("ProjectFilter", "Project", ProjectName, $$"""({\"name\":\"{{ProjectName}}\"})"""),
			("DeckFilter", "Deck", DeckTitle, $$"""({\"title\":\"{{DeckTitle}}\"})"""),
			("AssigneeFilter", "AssigneeEmail", AssigneeEmail,
				$$"""\"primaryEmail\":{\"email\":\"{{AssigneeEmail}}\"}"""),
		};

		foreach (var (filterName, valueName, value, filterValue) in replacements)
		{
			query = query.Replace($"<{filterName}>",
				string.IsNullOrWhiteSpace(value) ? "" : filterValue.Replace($"<{valueName}>", value));
		}

		return query;
	}
}