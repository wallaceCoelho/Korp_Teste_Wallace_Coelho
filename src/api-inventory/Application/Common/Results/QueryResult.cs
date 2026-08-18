using Application.Common.Errors;

namespace Application.Common.Results;

public readonly struct QueryResult<TSuccess>
{
    private readonly TSuccess? _value;
    private readonly ApiError? _apiError;

    public bool IsSuccess { get; }
    public bool IsApiError => !IsSuccess;

    private QueryResult(TSuccess value)
    {
        _value = value;
        _apiError = null;
        IsSuccess = true;
    }

    private QueryResult(ApiError apiError)
    {
        _value = default;
        _apiError = apiError;
        IsSuccess = false;
    }

    public static implicit operator QueryResult<TSuccess>(TSuccess value) => new(value);
    public static implicit operator QueryResult<TSuccess>(ApiError apiError) => new(apiError);

    public TResult Match<TResult>(Func<TSuccess, TResult> onSuccess, Func<ApiError, TResult> onApiError)
    {
        if (IsSuccess) return onSuccess(_value!);
        return onApiError(_apiError!);
    }
}
