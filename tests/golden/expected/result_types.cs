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
/// </summary>
/// <param name="a">Parameter of type int</param>
/// <param name="b">Parameter of type int</param>
/// <returns>Returns Result<int, string></returns>
public static Result<int, string> divide(int a, int b)
{
    if (b == 0)
    {
        return Result.Error("Division by zero");
    }
    else
    {
        return Result.Ok(a / b);
    }
}

/// <summary>
/// </summary>
/// <param name="input">Parameter of type string</param>
/// <returns>Returns Result<int, string></returns>
public static Result<int, string> processData(string input)
{
    var result_result = parseNumber(input);
    if (result_result.IsError)
        return result_result;
    var result = result_result.Value;
    return Result.Ok(result * 2);
}

/// <summary>
/// </summary>
/// <param name="x">Parameter of type int</param>
/// <returns>Returns Result<string, string></returns>
public static Result<string, string> chainResults(int x)
{
    var doubled_result = doubleValue(x);
    if (doubled_result.IsError)
        return doubled_result;
    var doubled = doubled_result.Value;
    var processed_result = processValue(doubled);
    if (processed_result.IsError)
        return processed_result;
    var processed = processed_result.Value;
    return Result.Ok(processed);
}