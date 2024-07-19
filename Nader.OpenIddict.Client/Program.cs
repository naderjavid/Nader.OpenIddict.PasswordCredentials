using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Client;
using static OpenIddict.Abstractions.OpenIddictConstants;

var services = new ServiceCollection();

services.AddOpenIddict()

    // Register the OpenIddict client components.
    .AddClient(options =>
    {
        // Allow grant_type=password and grant_type=refresh_token to be negotiated.
        options.AllowPasswordFlow()
               .AllowRefreshTokenFlow();

        // Disable token storage, which is not necessary for non-interactive flows like
        // grant_type=password, grant_type=client_credentials or grant_type=refresh_token.
        options.DisableTokenStorage();

        // Register the System.Net.Http integration and use the identity of the current
        // assembly as a more specific user agent, which can be useful when dealing with
        // providers that use the user agent as a way to throttle requests (e.g Reddit).
        options.UseSystemNetHttp()
               .SetProductInformation(typeof(Program).Assembly);

        // Add a client registration without a client identifier/secret attached.
        options.AddRegistration(new OpenIddictClientRegistration
        {
            Issuer = new Uri("https://localhost:7033/", UriKind.Absolute),
            ClientId = "console",
            ClientSecret = "388D45FA-B36B-4988-BA59-B187D329C207",
        });
    });

await using var provider = services.BuildServiceProvider();

const string adminEmail = "nader@test.com", password = "1q2w3E##";
const string userEmail = "user@test.com";

await CreateAccountAsync(provider, adminEmail, password, "admin");
await CreateAccountAsync(provider, userEmail, password);

var tokens = await GetTokensAsync(provider, adminEmail, password);
Console.WriteLine("Admin user access token: {0}", tokens.AccessToken);
Console.WriteLine();
Console.WriteLine("Admin user refresh token: {0}", tokens.RefreshToken);

Console.WriteLine();
Console.WriteLine();

await GetSecureResource(provider, adminEmail, tokens.AccessToken);

Console.WriteLine();
Console.WriteLine();

await GetSecureByRoleResource(provider, adminEmail, tokens.AccessToken);

Console.WriteLine();
Console.WriteLine();

tokens = await GetTokensAsync(provider, userEmail, password);
Console.WriteLine("Admin user access token: {0}", tokens.AccessToken);
Console.WriteLine();
Console.WriteLine("Admin user refresh token: {0}", tokens.RefreshToken);


await GetSecureResource(provider, userEmail, tokens.AccessToken);

Console.WriteLine();
Console.WriteLine();

await GetSecureByRoleResource(provider, userEmail, tokens.AccessToken);

Console.WriteLine();
Console.WriteLine();

tokens = await RefreshTokensAsync(provider, tokens.RefreshToken);
Console.WriteLine("New access token: {0}", tokens.AccessToken);
Console.WriteLine();
Console.WriteLine("New refresh token: {0}", tokens.RefreshToken);
Console.WriteLine();

Console.ReadLine();

static async Task GetSecureResource(IServiceProvider provider, string userName, string token)
{
    using var client = provider.GetRequiredService<HttpClient>();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    var response = await client.GetAsync("https://localhost:7033/secure");

    Console.WriteLine();
    Console.WriteLine();

    Console.WriteLine($"Api Address: '/secure'         || UserName: {userName} || Status Code: {response.StatusCode} || Response: {await response.Content.ReadAsStringAsync()}");
}

static async Task GetSecureByRoleResource(IServiceProvider provider, string userName, string token)
{
    using var client = provider.GetRequiredService<HttpClient>();
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    var response = await client.GetAsync("https://localhost:7033/secure-by-role");

    Console.WriteLine();
    Console.WriteLine();

    Console.WriteLine($"Api Address: '/secure-by-role' || UserName: {userName} || Status Code: {response.StatusCode} || Response: {await response.Content.ReadAsStringAsync()}");
}

static async Task CreateAccountAsync(IServiceProvider provider, string email, string password, string? role = null)
{
    using var client = provider.GetRequiredService<HttpClient>();
    var response = await client.PostAsJsonAsync("https://localhost:7033/account/register", new { email, password, role });

    // Ignore 409 responses, as they indicate that the account already exists.
    if (response.StatusCode == HttpStatusCode.Conflict)
    {
        return;
    }

    response.EnsureSuccessStatusCode();
}

static async Task<(string AccessToken, string RefreshToken)> GetTokensAsync(IServiceProvider provider, string email, string password)
{
    var service = provider.GetRequiredService<OpenIddictClientService>();

    // Note: the "offline_access" scope must be requested and granted to receive a refresh token.
    var result = await service.AuthenticateWithPasswordAsync(new()
    {
        Username = email,
        Password = password,
        Scopes = new() { Scopes.OfflineAccess }
    });

    return (result.AccessToken, result.RefreshToken);
}

static async Task<(string AccessToken, string RefreshToken)> RefreshTokensAsync(IServiceProvider provider, string token)
{
    var service = provider.GetRequiredService<OpenIddictClientService>();

    var result = await service.AuthenticateWithRefreshTokenAsync(new()
    {
        RefreshToken = token
    });

    return (result.AccessToken, result.RefreshToken);
}
