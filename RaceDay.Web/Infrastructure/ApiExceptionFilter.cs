using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using RaceDay.Web.Models.ViewModels;
using RaceDay.Web.Services;

namespace RaceDay.Web.Infrastructure;

/// <summary>
/// Turns failed API calls into a friendly page or message instead of a crash or a raw error:
///  - an expired session sends the user back to the login page,
///  - a failed page load (GET) shows the shared error page with the API's own message,
///  - a failed action (POST) returns to the page the user came from with the message shown.
/// Controllers that want to keep a form on screen (login, forms) catch ApiException themselves.
/// </summary>
public class ApiExceptionFilter : IAsyncExceptionFilter
{
    private readonly ITempDataDictionaryFactory _tempDataFactory;
    private readonly IModelMetadataProvider _metadataProvider;
    private readonly ILogger<ApiExceptionFilter> _logger;

    public ApiExceptionFilter(
        ITempDataDictionaryFactory tempDataFactory,
        IModelMetadataProvider metadataProvider,
        ILogger<ApiExceptionFilter> logger)
    {
        _tempDataFactory = tempDataFactory;
        _metadataProvider = metadataProvider;
        _logger = logger;
    }

    public async Task OnExceptionAsync(ExceptionContext context)
    {
        if (context.Exception is not ApiException exception)
        {
            return;
        }

        var http = context.HttpContext;
        var isGet = HttpMethods.IsGet(http.Request.Method);

        if (exception is ApiUnauthorizedException)
        {
            await UserSession.SignOutAsync(http);
            _tempDataFactory.GetTempData(http)["Warning"] = "Your session has ended. Please log in again.";

            var returnUrl = isGet ? $"{http.Request.Path}{http.Request.QueryString}" : null;
            context.Result = new RedirectToActionResult("Login", "Account", returnUrl is null ? null : new { returnUrl });
            context.ExceptionHandled = true;
            return;
        }

        _logger.LogWarning("An API call failed with status {Status}: {Message}", (int)exception.StatusCode, exception.Message);

        if (isGet)
        {
            context.Result = new ViewResult
            {
                ViewName = "Error",
                StatusCode = (int)exception.StatusCode,
                ViewData = new ViewDataDictionary<ErrorViewModel>(_metadataProvider, context.ModelState)
                {
                    Model = new ErrorViewModel
                    {
                        StatusCode = (int)exception.StatusCode,
                        Message = exception.Message
                    }
                }
            };
        }
        else
        {
            _tempDataFactory.GetTempData(http)["Error"] = exception.Message;
            context.Result = new LocalRedirectResult(SafeReferer(http));
        }

        context.ExceptionHandled = true;
    }

    private static string SafeReferer(HttpContext http)
    {
        var referer = http.Request.Headers.Referer.ToString();

        if (Uri.TryCreate(referer, UriKind.Absolute, out var uri) &&
            string.Equals(uri.Authority, http.Request.Host.Value, StringComparison.OrdinalIgnoreCase))
        {
            return uri.PathAndQuery;
        }

        return "/";
    }
}
