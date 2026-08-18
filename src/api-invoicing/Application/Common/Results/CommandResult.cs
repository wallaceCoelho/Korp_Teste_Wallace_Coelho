using Application.Common.Errors;

namespace Application.Common.Results;

public readonly struct CommandResult<TSuccess>
{
    private readonly TSuccess? _value;
    private readonly ApiError? _apiError;
    private readonly ValidationError? _validationError;

    public bool IsSuccess { get; }
    public bool IsApiError { get; }
    public bool IsValidationError { get; }

    private CommandResult(TSuccess value)
    {
        _value = value;
        _apiError = null;
        _validationError = null;
        IsSuccess = true;
        IsApiError = false;
        IsValidationError = false;
    }

    private CommandResult(ApiError apiError)
    {
        _value = default;
        _apiError = apiError;
        _validationError = null;
        IsSuccess = false;
        IsApiError = true;
        IsValidationError = false;
    }

    private CommandResult(ValidationError validationError)
    {
        _value = default;
        _apiError = null;
        _validationError = validationError;
        IsSuccess = false;
        IsApiError = false;
        IsValidationError = true;
    }

    public static implicit operator CommandResult<TSuccess>(TSuccess value) => new(value);
    public static implicit operator CommandResult<TSuccess>(ApiError apiError) => new(apiError);
    public static implicit operator CommandResult<TSuccess>(ValidationError validationError) => new(validationError);

    public TResult Match<TResult>(Func<TSuccess, TResult> onSuccess, Func<ApiError, TResult> onApiError, Func<ValidationError, TResult> onValidationError)
    {
        if (IsSuccess) return onSuccess(_value!);
        if (IsApiError) return onApiError(_apiError!);
        if (IsValidationError) return onValidationError(_validationError!);

        throw new InvalidOperationException("Estado de CommandResult inválido.");
    }
}
