namespace Xarbrough.CodecksPlasticIntegration.Tests;

public class CardIdConverterTests
{
	[TestCaseSource(nameof(testCases))]
	public void SeqToInt_ValidString_ReturnsValidNumber(string seq, int number)
	{
		Assert.AreEqual(number, new CardIdConverter().SeqToInt(seq));
	}

	[TestCaseSource(nameof(testCases))]
	public void IntToSeq_ValidNumber_ReturnsValidString(string seq, int number)
	{
		Assert.AreEqual(seq, new CardIdConverter().IntToSeq(number));
	}

	/// <summary>
	/// These are some valid mappings that were retrieved by comparing
	/// the json data sent by the Codecks API (containing the sequential ID)
	/// with the card ID label displayed in the web app.
	/// </summary>
	private static readonly object[] testCases =
	{
		new object[] { "13c", 67 },
		new object[] { "1as", 274 },
		new object[] { "1ca", 290 },
		new object[] { "1k5", 481 },
		new object[] { "1uv", 640 },
		new object[] { "1s2", 590 },
	};
}
