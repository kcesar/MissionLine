namespace Kcesar.MissionLine.Website.Controllers
{
  using System.Threading.Tasks;
  using System.Web;
  using System.Web.Mvc;
  using Data;
  using Identity;
  using Microsoft.AspNet.Identity;
  using Microsoft.AspNet.Identity.Owin;
  using Microsoft.Owin.Security;

  [Authorize]
  public class AccountController : Controller
  {
    private ApplicationSignInManager signInManager;
    private ApplicationUserManager userManager;
    private IMemberSource memberSource;

    public AccountController()
    {
    }

    public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager, IMemberSource memberSource)
    {
      UserManager = userManager;
      SignInManager = signInManager;
      MemberSource = memberSource;
    }

    public IMemberSource MemberSource
    {
      get
      {
        return this.memberSource ?? new MemberSource(new ConfigSource());
      }
      private set
      {
        this.memberSource = value;
      }
    }

    public ApplicationSignInManager SignInManager
    {
      get
      {
        return signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
      }
      private set
      {
        signInManager = value;
      }
    }

    public ApplicationUserManager UserManager
    {
      get
      {
        return userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
      }
      private set
      {
        userManager = value;
      }
    }

    //
    // GET: /Account/Login
    [AllowAnonymous]
    public ActionResult Login(string returnUrl)
    {
      ViewBag.ReturnUrl = returnUrl;
      return View();
    }

    //
    // POST: /Account/ExternalLogin
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public ActionResult ExternalLogin(string provider, string returnUrl)
    {
      // Request a redirect to the external login provider
      return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
    }

    public ActionResult Signout()
    {
      AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie, DefaultAuthenticationTypes.ExternalCookie);
      return Redirect(Url.Content("~/Account/Login"));
    }

    //
    // GET: /Account/ExternalLoginCallback
    [AllowAnonymous]
    public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
    {
      var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
      if (loginInfo == null)
      {
        return RedirectToAction("Login");
      }

      // Sign in the user with this external login provider if the user already has a login
      var result = await SignInManager.ExternalSignInAsync(loginInfo, isPersistent: false);
      switch (result)
      {
        case SignInStatus.Success:
          return RedirectToLocal(returnUrl);
        case SignInStatus.LockedOut:
          //return View("Lockout");
          return Content("System states account is locked out. This is not currently supported");
        case SignInStatus.RequiresVerification:
          //return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });
          return Content("System requires verification. This is not currently supported");
        case SignInStatus.Failure:
        default:
          if (loginInfo.Login.LoginProvider.StartsWith("https://sts.windows.net/"))
          {
            return await GetUserAddLoginAndSignIn(false, loginInfo.DefaultUserName, loginInfo, returnUrl);
          }
          else
          {
            var username = await this.MemberSource.LookupExternalLogin(loginInfo.Login.LoginProvider, loginInfo.Login.ProviderKey);
            if (username == null)
            {
              return View("RegisterLogin");
            }

            return await GetUserAddLoginAndSignIn(true, username, loginInfo, returnUrl);
          }
      }
    }

    private async Task<ActionResult> GetUserAddLoginAndSignIn(bool findUser, string username, ExternalLoginInfo loginInfo, string returnUrl)
    {
      // If the user does not have an account, then prompt the user to create an account
      ApplicationUser user = null;
      if (findUser)
      {
        user = await UserManager.FindByNameAsync(username);
      }

      if (user == null)
      {
        user = new ApplicationUser { UserName = username };
        var createResult = await UserManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
          return View("RegistrationError");
        }
      }

      var addLoginResult = await UserManager.AddLoginAsync(user.Id, loginInfo.Login);      if (!addLoginResult.Succeeded)      {
        return View("RegistrationError");
      }      
      await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
      return RedirectToLocal(returnUrl);
    }


    //
    // GET: /Account/ExternalLoginFailure
    [AllowAnonymous]
    public ActionResult ExternalLoginFailure()
    {
      return View();
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        if (userManager != null)
        {
          userManager.Dispose();
          userManager = null;
        }

        if (signInManager != null)
        {
          signInManager.Dispose();
          signInManager = null;
        }
      }

      base.Dispose(disposing);
    }

    #region Helpers
    // Used for XSRF protection when adding external logins
    private const string XsrfKey = "XsrfId";

    private IAuthenticationManager AuthenticationManager
    {
      get
      {
        return HttpContext.GetOwinContext().Authentication;
      }
    }

    private ActionResult RedirectToLocal(string returnUrl)
    {
      if (Url.IsLocalUrl(returnUrl))
      {
        return Redirect(returnUrl);
      }
      return RedirectToAction("Index", "Home");
    }

    internal class ChallengeResult : HttpUnauthorizedResult
    {
      public ChallengeResult(string provider, string redirectUri)
          : this(provider, redirectUri, null)
      {
      }

      public ChallengeResult(string provider, string redirectUri, string userId)
      {
        LoginProvider = provider;
        RedirectUri = redirectUri;
        UserId = userId;
      }

      public string LoginProvider { get; set; }
      public string RedirectUri { get; set; }
      public string UserId { get; set; }

      public override void ExecuteResult(ControllerContext context)
      {
        var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
        if (UserId != null)
        {
          properties.Dictionary[XsrfKey] = UserId;
        }
        context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
      }
    }
    #endregion
  }
}