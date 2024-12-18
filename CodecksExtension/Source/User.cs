namespace Xarbrough.CodecksPlasticIntegration;

/// <summary>
/// A data transfer object that represents 
/// the relevant properties of a Codecks user.
/// </summary>
public struct User
{
	public readonly string id;
	public readonly string email;

	public User(string id, string email)
	{
		this.id = id;
		this.email = email;
	}
}