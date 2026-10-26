using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudySpace.Web.Models;

namespace StudySpace.Web.Controllers;

public sealed class AccountController(IConfiguration configuration, IWebHostEnvironment environment) : Controller
{
    [AllowAnonymous]
    public IActionResult Login() => View(new LoginViewModel
    {
        GoogleEnabled = !string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientId"])
    });

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult GoogleLogin(string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(configuration["Authentication:Google:ClientId"]))
        {
            return BadRequest("Google OAuth is not configured.");
        }

        return Challenge(new AuthenticationProperties
        {
            RedirectUri = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action("Index", "Home")
        }, "Google");
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DevelopmentLogin(LoginViewModel model)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View("Login", model);
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, model.Subject),
            new Claim(ClaimTypes.Name, model.DisplayName)
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        return RedirectToAction("Index", "Workspaces");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    public IActionResult Denied() => View();
}

