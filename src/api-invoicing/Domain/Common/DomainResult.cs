namespace Domain.Common;

public readonly record struct DomainResult
{
    public bool IsSuccess { get; }
    public string? Error { get; }

    private DomainResult(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static DomainResult Success() => new(true, null);
    public static DomainResult Failure(string error) => new(false, error);

    public static implicit operator DomainResult(string error) => Failure(error);
}

public readonly record struct DomainResult<TValue>
{
    public bool IsSuccess { get; }
    public TValue? Value { get; }
    public string? Error { get; }

    private DomainResult(bool isSuccess, TValue? value, string? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static DomainResult<TValue> Success(TValue value) => new(true, value, null);
    public static DomainResult<TValue> Failure(string error) => new(false, default, error);

    public static implicit operator DomainResult<TValue>(string error) => Failure(error);
    public static implicit operator DomainResult<TValue>(TValue value) => Success(value);
}
