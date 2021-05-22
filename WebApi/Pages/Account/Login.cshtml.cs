using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApi.Application;

namespace WebApi.Pages.Account
{
    [AllowAnonymous]
    [BindProperties(SupportsGet=true)]
    public class Login : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        public string UserName { get; set; }
        public string ReturnUrl { get; set; }

        public Login(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = new ApplicationUser() {UserName = UserName};
            await _userManager.CreateAsync(user);
            await _signInManager.SignInAsync(user, true);
            return Redirect(ReturnUrl);
        }
    }
}