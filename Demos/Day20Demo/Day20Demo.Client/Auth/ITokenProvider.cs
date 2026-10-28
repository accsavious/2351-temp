namespace Day20Demo.Client.Auth;

// Hand back the token or return null
public interface ITokenProvider
{
    Task<string> GetTokenAsync();
}