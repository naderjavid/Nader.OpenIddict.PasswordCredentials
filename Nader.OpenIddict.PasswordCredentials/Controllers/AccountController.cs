using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Nader.OpenIddict.PasswordCredentials.Contracts;
using Nader.OpenIddict.PasswordCredentials.Database;
using Nader.OpenIddict.PasswordCredentials.Entities;

namespace Nader.OpenIddict.PasswordCredentials.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class AccountController : Controller
{
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _applicationDbContext;
    private static bool _databaseChecked;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext applicationDbContext,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _applicationDbContext = applicationDbContext;
        _roleManager = roleManager;
    }

    //
    // POST: /Account/Register
    [HttpPost("Register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] CreateUserRequest input)
    {
        EnsureDatabaseCreated(_applicationDbContext);
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByNameAsync(input.Email);
            if (user != null)
            {
                return StatusCode(StatusCodes.Status409Conflict);
            }

            user = new ApplicationUser { UserName = input.Email, Email = input.Email };
            var result = await _userManager.CreateAsync(user, input.Password);
            if (result.Succeeded)
            {
                if(input.Role is not null) await _userManager.AddToRoleAsync(user, input.Role);
                return Ok();
            }
            AddErrors(result);
        }

        // If we got this far, something failed.
        return BadRequest(ModelState);
    }

    #region Helpers

    // The following code creates the database and schema if they don't exist.
    // This is a temporary workaround since deploying database through EF migrations is
    // not yet supported in this release.
    // Please see this http://go.microsoft.com/fwlink/?LinkID=615859 for more information on how to do deploy the database
    // when publishing your application.
    private void EnsureDatabaseCreated(ApplicationDbContext context)
    {
        _roleManager.CreateAsync(new IdentityRole("admin"));
        if (!_databaseChecked)
        {
            _databaseChecked = true;
            context.Database.EnsureCreated();
        }
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    #endregion
}
