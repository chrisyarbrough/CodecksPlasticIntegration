namespace Xarbrough.CodecksPlasticIntegration;

/// <summary>
/// A utility to access configurable files that are published next to the assembly.
/// </summary>
static class Resources
{
	public static string ReadAllText(string relativePath)
	{
		string directory = Path.GetDirectoryName(typeof(CodecksExtension).Assembly.Location);
		return File.ReadAllText(directory + "/" + relativePath);
	}
}