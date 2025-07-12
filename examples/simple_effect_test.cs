public class Result<T, E>
{
    public T Value { get; private set; }
    public E ErrorValue { get; private set; }
    public bool IsError { get; private set; }

    public static Result<T, E> Ok(T value)
    {
        return new Result<T, E>
        {
            Value = value,
            IsError = false
        };
    }

    public static Result<T, E> Error(E error)
    {
        return new Result<T, E>
        {
            ErrorValue = error,
            IsError = true
        };
    }
}

/// <summary>
/// Pure function - no side effects
/// </summary>
/// <param name="a">Parameter of type int</param>
/// <param name="b">Parameter of type int</param>
/// <returns>Returns int</returns>
public static int add(int a, int b)
{
    return a + b;
}

/// <summary>
/// Effects: Logging
/// </summary>
/// <param name="msg">Parameter of type string</param>
/// <returns>Returns Result<string, string></returns>
public static Result<string, string> log_info(string msg)
{
    return Result.Ok("Logged: " + msg);
}

/// <summary>
/// Effects: Database, Logging
/// </summary>
/// <param name="data">Parameter of type string</param>
/// <returns>Returns Result<string, string></returns>
public static Result<string, string> save_and_log(string data)
{
    var log_result_result = log_info("Saving: " + data);
    if (log_result_result.IsError)
        return log_result_result;
    var log_result = log_result_result.Value;
    return Result.Ok("Saved: " + data);
}

/// <summary>
/// Effects: Database, Network, Logging, FileSystem, Memory, IO
/// </summary>
/// <param name="input">Parameter of type string</param>
/// <returns>Returns Result<string, string></returns>
public static Result<string, string> complex_operation(string input)
{
    var result_result = save_and_log(input);
    if (result_result.IsError)
        return result_result;
    var result = result_result.Value;
    return Result.Ok("Complex operation completed: " + input);
}