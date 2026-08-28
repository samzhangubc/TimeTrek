namespace ThymeMe.Application.Common;

public sealed record OperationError(string Code, string Message);

public sealed record OperationResult<T>(T? Value, OperationError? Error)
{
    public bool IsSuccess => Error is null;
}

public static class OperationResult
{
    public static OperationResult<T> Success<T>(T value) => new(value, null);

    public static OperationResult<T> Failure<T>(string code, string message) =>
        new(default, new OperationError(code, message));
}
