namespace Xarbrough.CodecksPlasticIntegration;

using System.Text;

class Query
{
	public static string Load(string fileName)
	{
		return Minimize(
			Resources.ReadAllText($"Queries/{fileName}")
				.Replace("'", "\\\""));
	}

	private static string Minimize(string json)
	{
		StringBuilder stringBuilder = new();
		foreach (char c in json.Where(c =>
			         !char.IsSeparator(c) &&
			         !char.IsControl(c)))
		{
			stringBuilder.Append(c);
		}

		return stringBuilder.ToString();
	}
}