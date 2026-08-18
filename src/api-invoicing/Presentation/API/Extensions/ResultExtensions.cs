using Application.Common.Errors;
using Application.Common.Results;

namespace Presentation.API.Extensions;

public static class ResultExtensions
{
    public static IResult ToHttpResult<T>(this CommandResult<T> result)
        => result.ToHttpResult(value => Results.Ok(value));

    public static IResult ToHttpResult<T>(this CommandResult<T> result, Func<T, IResult> onSuccess)
        => result.Match(
            onSuccess: value => onSuccess(value),
            onApiError: error => MapApiError(error),
            onValidationError: validation => Results.UnprocessableEntity(validation)
        );

    public static IResult ToHttpResult<T>(this QueryResult<T> result)
        => result.ToHttpResult(value => Results.Ok(value));

    public static IResult ToHttpResult<T>(this QueryResult<T> result, Func<T, IResult> onSuccess)
        => result.Match(
            onSuccess: value => onSuccess(value),
            onApiError: error => MapApiError(error)
        );

    private static IResult MapApiError(ApiError error) => error.Type switch
    {
        ErrorType.NotFound => Results.NotFound(new { error.Message }),
        ErrorType.Conflict => Results.Conflict(new { error.Message }),
        ErrorType.Unauthorized => Results.Unauthorized(),
        ErrorType.Forbidden => Results.Forbid(),
        _ => Results.BadRequest(new { error.Message })
    };
}
