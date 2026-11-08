using System.Net;

namespace RaceDay.Web.Services;

/// <summary>
/// A failed call to the RaceDay API. <see cref="Exception.Message"/> is always safe to show to a
/// user: it is either the API's own message (e.g. "This category is full.") or a friendly fallback.
/// </summary>
public class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public ApiException(HttpStatusCode statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }
}

/// <summary>The API rejected the call because there is no valid session (HTTP 401).</summary>
public class ApiUnauthorizedException : ApiException
{
    public ApiUnauthorizedException(string message) : base(HttpStatusCode.Unauthorized, message)
    {
    }
}
