namespace Application.Common.Errors;

public enum ErrorType
{
    BadRequest,
    NotFound,
    Conflict,
    Forbidden,
    Unauthorized
}

public sealed record ApiError(ErrorType Type, string Message);

public sealed record ValidationError(List<ValidationErrorItem> Errors);
public sealed record ValidationErrorItem(string Property, string Message);
