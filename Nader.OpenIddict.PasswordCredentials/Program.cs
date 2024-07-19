using Microsoft.AspNetCore.Authorization;
using Nader.OpenIddict.PasswordCredentials.Entities;
using Nader.OpenIddict.PasswordCredentials.Extensions;
using OpenIddict.Validation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddIdentityAndOpenIddict(builder.Configuration);

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthorization();

var app = builder.Build();

app.MapIdentityApi<ApplicationUser>();

app.MapGet("/secure", [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)] () =>
{
    return "You're authenticated!!";
});


app.MapGet("/secure-by-role", () =>
{
    return "You're authorized!!";
})
.RequireAuthorization(builder =>
{
    builder.AuthenticationSchemes.Add(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
    builder.RequireRole("admin");
});

app.MapGet("/Account/Login", () =>
{
    return "You don't have access!!";
});


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
