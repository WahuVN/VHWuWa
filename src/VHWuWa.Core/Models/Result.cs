namespace VHWuWa.Core.Models;

/// <summary>Kết quả thao tác không có giá trị trả về.</summary>
public class Result
{
    public bool Success { get; init; }
    public string? Error { get; init; }
    public Exception? Exception { get; init; }

    public static Result Ok() => new() { Success = true };
    public static Result Fail(string error, Exception? ex = null) =>
        new() { Success = false, Error = error, Exception = ex };
}

/// <summary>Kết quả thao tác có giá trị trả về.</summary>
public sealed class Result<T> : Result
{
    public T? Value { get; init; }

    public static Result<T> Ok(T value) => new() { Success = true, Value = value };
    public static new Result<T> Fail(string error, Exception? ex = null) =>
        new() { Success = false, Error = error, Exception = ex };
}
