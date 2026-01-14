using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using System.DirectoryServices.AccountManagement; // 1. *** Library ที่ทำให้เกิด Warning ***
using System.Security.Claims;
using CIS.ViewModels;
using System.Security.Principal;

namespace CIS.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly IConfiguration _configuration;

        public AccountController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Login(string? returnUrl)
        {
            var model = new LoginViewModel
            {
                ReturnUrl = returnUrl ?? Url.Content("~/")
            };

            // 2. *** เราจะ "ปิด" คำเตือน CA1416 ชั่วคราว ***
#pragma warning disable CA1416 
            var result = await HttpContext.AuthenticateAsync(NegotiateDefaults.AuthenticationScheme);

            if (result.Succeeded && result.Principal != null)
            {
                var windowsIdentity = result.Principal.Identity as ClaimsIdentity;
                if (windowsIdentity != null && !string.IsNullOrEmpty(windowsIdentity.Name))
                {
                    model.Username = windowsIdentity.Name;
                    model.IsWindowsUserPreFilled = true;
                }
            }
#pragma warning restore CA1416 // 3. *** เปิดคำเตือนกลับมา (เผื่อโค้ดอื่น) ***

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                string domain = "CRIMCAD";
                string username = model.Username;

                if (username.Contains('\\'))
                {
                    var parts = username.Split('\\');
                    domain = parts[0];
                    username = parts[1];
                }

#pragma warning disable CA1416 // 4. *** ปิดคำเตือนชุดใหญ่ ***
                using (var context = new PrincipalContext(ContextType.Domain, domain))
                {
                    bool isValid = context.ValidateCredentials(username, model.Password);

                    if (isValid)
                    {
                        await SignInUser(username, domain); // (Helper นี้ก็ถูกปิด Warning ด้วย)
                        return LocalRedirect(model.ReturnUrl);
                    }
                }
#pragma warning restore CA1416
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "เกิดข้อผิดพลาดในการเชื่อมต่อ AD");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Username หรือ รหัสผ่าน ไม่ถูกต้องนะจ๊ะ!");
            return View(model);
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        // [HELPER] สร้าง Cookie
#pragma warning disable CA1416 // 5. *** ปิดคำเตือนให้ Helper นี้ด้วย ***
        private async Task SignInUser(string username, string domain)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, $"{domain.ToUpper()}\\{username}")
            };

            try
            {
                using (var context = new PrincipalContext(ContextType.Domain, domain))
                using (var userPrincipal = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username))
                {
                    if (userPrincipal != null)
                    {
                        var groups = userPrincipal.GetAuthorizationGroups();
                        foreach (var group in groups)
                        {
                            if (group.Sid.IsWellKnown(WellKnownSidType.BuiltinUsersSid)) continue;
                            claims.Add(new Claim(ClaimTypes.Role, $"{domain.ToUpper()}\\{group.Name}"));
                        }
                    }
                }
            }
            catch (Exception) { /* (Log ex) */ }
#pragma warning restore CA1416

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties { };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }
    }
}