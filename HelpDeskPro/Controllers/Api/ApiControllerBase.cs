using HelpDeskPro.Services.Common;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskPro.Controllers.Api
{
    // Base class of all REST API controllers.
    //  [ApiController]          -> invalid input returns HTTP 400 automatically
    //  [IgnoreAntiforgeryToken] -> API calls use JSON + the login cookie (SameSite=Lax),
    //                              so they do not need the form anti-forgery token
    [ApiController]
    [IgnoreAntiforgeryToken]
    [Produces("application/json")]
    public abstract class ApiControllerBase : ControllerBase
    {
        protected UserContext CurrentUser
        {
            get { return UserContext.FromPrincipal(User); }
        }

        // Converts a failed ServiceResult into the right HTTP error.
        // Consistent errors: 400 / 403 / 404 / 409 / 503 (401, 429 and 500 come from the framework / middleware)
        protected ActionResult ErrorResult(ServiceResult result)
        {
            int statusCode = 500;
            if (result.ErrorType == ErrorType.Validation)
            {
                statusCode = 400;
            }
            else if (result.ErrorType == ErrorType.NotFound)
            {
                statusCode = 404;
            }
            else if (result.ErrorType == ErrorType.Forbidden)
            {
                statusCode = 403;
            }
            else if (result.ErrorType == ErrorType.Conflict)
            {
                statusCode = 409;
            }
            else if (result.ErrorType == ErrorType.Unavailable)
            {
                statusCode = 503;
            }

            return Problem(detail: result.Message, statusCode: statusCode);
        }
    }
}
