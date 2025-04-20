using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

public class BaseController : Controller
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        // If user is NOT authenticated
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            var controllerName = context.RouteData.Values["controller"]?.ToString();
            var actionName = context.RouteData.Values["action"]?.ToString();

            // Do NOT redirect if already on login, access denied, or similar public pages
            if (!(controllerName == "Account" && (actionName == "Login" || actionName == "AccessDenied")))
            {
                context.Result = RedirectToAction("Login", "Account");
            }
        }

        base.OnActionExecuting(context);
    }
}
