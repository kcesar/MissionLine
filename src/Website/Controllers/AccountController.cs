/*
 * Copyright 2015 Matthew Cosand
 */
namespace Kcesar.MissionLine.Website.Controllers
{
  using System;
  using System.IO;
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

    /// <summary>
    ///  Production constructor
    /// </summary>
    /// <param name="memberSource"></param>
    public AccountController(IMemberSource memberSource)
    {
      this.memberSource = memberSource;
    }

    public AccountController(ApplicationUserManager userManager, ApplicationSignInManager signInManager, IMemberSource memberSource)
      : this(memberSource)
    {
      UserManager = userManager;
      SignInManager = signInManager;
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
      return View("Login");
    }

    [HttpGet]
    public async Task<ActionResult> Link()
    {
      var user = await UserManager.FindByNameAsync(User.Identity.Name);
      user.LinkCode = (Path.GetRandomFileName() + Path.GetRandomFileName() + Path.GetRandomFileName()).Replace(".", "");
      user.LinkCodeExpires = DateTime.Now.AddMinutes(10);
      var saveResult = await UserManager.UpdateAsync(user);
      if (saveResult.Succeeded)
      {
        ViewBag.LinkCode = "@" + user.LinkCode;
        return View();
      }
      return Content(string.Join("\n", saveResult.Errors));
    }

    [Authorize]
    public ActionResult LinkComplete(string provider)
    {
      ViewBag.Provider = provider;
      return View();
    }

    [Authorize]
    [HttpGet]
    public ActionResult LinkFailure(string reason)
    {
      ViewBag.Message = reason;
      return View("Error");
    }


    //
    // POST: /Account/ExternalLogin
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public ActionResult ExternalLogin(string provider, string returnUrl, string linkCode)
    {
      // Request a redirect to the external login provider
      return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl, LinkCode = linkCode }));
    }

    public ActionResult Signout()
    {
      AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie, DefaultAuthenticationTypes.ExternalCookie);
      return Redirect(Url.Content("~/Account/Login"));
    }

    //
    // GET: /Account/ExternalLoginCallback
    [AllowAnonymous]
    public async Task<ActionResult> ExternalLoginCallback(string returnUrl, string linkCode)
    {
      linkCode = linkCode ?? " ";
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
          if (linkCode[0] == '@')
          {
            return RedirectToLocal(Url.Action("LinkFailure", new { reason = "This login is already linked to user " + User.Identity.Name + "." }));
          }
          else
          {
            return RedirectToLocal(returnUrl);
          }
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
            ApplicationUser user = new ApplicationUser { UserName = loginInfo.DefaultUserName };
            var createResult = await UserManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
              ViewBag.Message = string.Format("Couldn't create user '{0}': {1}", user.UserName, string.Join(" / ", createResult.Errors));
              return View("Error");
            }
            return await AddLoginAndSignIn(user, loginInfo, returnUrl);
          }
          else if (linkCode[0] == '@')
          {
            var user = await this.UserManager.FindByLinkCodeAsync(linkCode.Substring(1));
            if (user == null)
            {
              return RedirectToLocal(Url.Action("LinkFailure", new { reason = "Link code not found, or was expired. Please try linking your login again." }));
            }

            return await AddLoginAndSignIn(user, loginInfo, Url.Action("LinkComplete", new { Provider = loginInfo.Login.LoginProvider }));
          }
          else
          {
            return Content("Login unknown. To use an external login you must first login with your ESAR Office 365 account, then go to page " + Url.Action("Link") + ".");
          }
      }
    }

    private async Task<ActionResult> AddLoginAndSignIn(ApplicationUser user, ExternalLoginInfo loginInfo, string returnUrl)
    {
      var addLoginResult = await UserManager.AddLoginAsync(user.Id, loginInfo.Login);
      if (!addLoginResult.Succeeded)
      {
        ViewBag.Message = "Couldn't setup user login: " + string.Join(" / ", addLoginResult.Errors);
        return View("Error");
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