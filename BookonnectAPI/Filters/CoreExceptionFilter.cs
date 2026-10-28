using BookonnectCore.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BookonnectAPI.Filters;

public sealed class CoreExceptionFilter : IExceptionFilter
{
    private readonly ILogger<CoreExceptionFilter> _logger;

    public CoreExceptionFilter(ILogger<CoreExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        var isCustomException = context.Exception is
            AccountLinkRequiredException or NotFoundException or AlreadyExistsException;
        var isV2Controller = context.ActionDescriptor is ControllerActionDescriptor action &&
            action.ControllerTypeInfo.Namespace == "BookonnectAPI.Controllers.V2";

        if (!isCustomException && !isV2Controller)
        {
            return;
        }

        var (statusCode, message) = context.Exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, context.Exception.Message),
            AccountLinkRequiredException or AlreadyExistsException =>
                (StatusCodes.Status409Conflict, context.Exception.Message),
            ArgumentException => (StatusCodes.Status400BadRequest, context.Exception.Message),
            TimeoutException => (StatusCodes.Status408RequestTimeout, "The request timed out. Please try again later."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.")
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(context.Exception, "Request failed with status {StatusCode}", statusCode);
        }
        else
        {
            _logger.LogWarning(context.Exception, "Request failed with status {StatusCode}", statusCode);
        }

        context.Result = new ObjectResult(new { Message = message })
        {
            StatusCode = statusCode
        };
        context.ExceptionHandled = true;
    }
}
