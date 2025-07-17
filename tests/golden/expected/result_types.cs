using System;

namespace Cadenza.Golden.ResultTypes
{

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

public static class CadenzaProgram
{
/// <summary>
/// </summary>
/// <param name="a">Parameter of type int</param>
/// <param name="b">Parameter of type int</param>
/// <returns>Returns Result<int, string></returns>
public static Result<int, string> divide(int a, int b)
{
    if (b == 0)
    {
        return Result<int, string>.Error("Division by zero");
    }
    else
    {
        return Result<int, string>.Ok(a / b);
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
    return Result<int, string>.Ok(result * 2);
}

/// <summary>
/// </summary>
/// <param name="x">Parameter of type int</param>
/// <returns>Returns Result<string, string></returns>
public static Result<string, string> chainResults(int x)
{
    var doubled_result = doubleValue(x);
    if (doubled_result.IsError)
        return Result<string, string>.Error(doubled_result.ErrorValue);
    var doubled = doubled_result.Value;
    var processed_result = processValue(doubled);
    if (processed_result.IsError)
        return processed_result;
    var processed = processed_result.Value;
    return Result<string, string>.Ok(processed);
}

/// <summary>
/// Helper function to parse a string to integer
/// </summary>
/// <param name="input">Parameter of type string</param>
/// <returns>Returns Result<int, string></returns>
public static Result<int, string> parseNumber(string input)
{
    if (int.TryParse(input, out int result))
    {
        return Result<int, string>.Ok(result);
    }
    else
    {
        return Result<int, string>.Error("Invalid number format");
    }
}

/// <summary>
/// Helper function to double a value
/// </summary>
/// <param name="x">Parameter of type int</param>
/// <returns>Returns Result<int, string></returns>
public static Result<int, string> doubleValue(int x)
{
    return Result<int, string>.Ok(x * 2);
}

/// <summary>
/// Helper function to process a value to string
/// </summary>
/// <param name="value">Parameter of type int</param>
/// <returns>Returns Result<string, string></returns>
public static Result<string, string> processValue(int value)
{
    return Result<string, string>.Ok($"processed: {value}");
}

}

}