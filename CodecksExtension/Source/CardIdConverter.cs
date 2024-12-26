namespace Xarbrough.CodecksPlasticIntegration;

/// <summary>
/// Cards in the Codecks web UI display a label such as '$24v'.
/// It is unclear how this label is called since it doesn't appear
/// in the API. However, it makes sense to use this user-facing label
/// and also display it in the Codecks Plastic extension.
///
/// Internally, the API uses two different values to identify cards:
/// 1) A 128-bit guid value called 'cardId'.
/// 2) A number called 'accountSeq'. This value appears to be unique
/// within the current account and is likely assigned to each card
/// in consecutive/sequential order.
///
/// This class converts between the UI display label and the accountSeq value.
/// </summary>
/// <remarks>
/// Since there is no official API to retrieve the display label, I asked
/// Daniel Berndt, one of the Codecks devs, for help. He posted this:
/// https://gist.github.com/danielberndt/eb59230c4ac5c2fd7edaa27dfb2b2e89
/// </remarks>
sealed class CardIdConverter
{
	private readonly string letters;
	private readonly int startVal;
	private readonly bool implicitZero;
	private readonly int length;
	private readonly Dictionary<char, int> letterToIndex;

	public CardIdConverter() : this(
		letters: "123456789acefghijkoqrsuvwxyz",
		startVal: 28 * 29 - 1,
		implicitZero: true) { }

	private CardIdConverter(string letters, int startVal, bool implicitZero)
	{
		this.letters = letters;
		this.startVal = startVal;
		this.implicitZero = implicitZero;
		length = letters.Length;

		letterToIndex = new Dictionary<char, int>();
		for (int i = 0; i < letters.Length; i++)
		{
			letterToIndex[letters[i]] = i;
		}
	}

	public int SeqToInt(string seq)
	{
		int intVal = letterToIndex.GetValueOrDefault(seq[0], 0);

		for (int i = 1; i < seq.Length; i += 1)
		{
			if (implicitZero)
				intVal += 1;

			intVal *= length;
			intVal += letterToIndex[seq[i]];
		}

		return intVal - startVal;
	}

	public string IntToSeq(int intVal)
	{
		string seq = string.Empty;
		int q = intVal + startVal;
		if (implicitZero)
			q += 1;
		do
		{
			if (implicitZero)
				q += -1;
			int r = q % length;
			q /= length;
			seq = letters[r] + seq;
		} while (q != 0);

		return seq;
	}
}
