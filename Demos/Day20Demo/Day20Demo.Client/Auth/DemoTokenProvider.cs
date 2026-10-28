using Day20Demo.Client.Auth;

namespace Day20Demo.Client.Auth;

// This demo only? : a fixed string, not a jwt

public class DemoTokenProvider : ITokenProvider
{
    public Task<string?> GetTokenAsync()
        => Task.FromResult<string?>("demo-bearer-token");
}