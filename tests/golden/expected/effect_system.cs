using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cadenza.Runtime;

public struct Result<T, E>
{
    public readonly bool IsSuccess;
    public readonly T Value;
    public readonly E Error;
    public Result(bool isSuccess, T value, E error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }
}

public static class Result
{
    public static Result<T, E> Ok<T, E>(T value)
    {
        return new Result<T, E>(true, value, default);
    }

    public static Result<T, E> Error<T, E>(E error)
    {
        return new Result<T, E>(false, default, error);
    }
}

public struct Option<T>
{
    public readonly bool HasValue;
    public readonly T Value;
    public Option(bool hasValue, T value)
    {
        HasValue = hasValue;
        Value = value;
    }
}

public static class Option
{
    public static Option<T> Some<T>(T value)
    {
        return new Option<T>(true, value);
    }

    public static Option<T> None<T>()
    {
        return new Option<T>(false, default);
    }
}

public static class CadenzaProgram
{
    /// <summary>
    /// Effects: Database, Logging
    /// </summary>
    /// <param name="name">Parameter of type string</param>
    /// <param name="email">Parameter of type string</param>
    /// <returns>Returns Result<int, string></returns>
    public static Result<int, string> saveUser(string name, string email)
    {
        var userId = generateId();
        return Result.Ok<object, string>(userId);
    }

    /// <summary>
    /// Effects: Network, Logging
    /// </summary>
    /// <param name="url">Parameter of type string</param>
    /// <returns>Returns Result<string, string></returns>
    public static Result<string, string> fetchData(string url)
    {
        return Result.Ok<object, string>("data");
    }

    /// <summary>
    /// Effects: FileSystem, Logging
    /// </summary>
    /// <param name="path">Parameter of type string</param>
    /// <returns>Returns Result<string, string></returns>
    public static Result<string, string> processFile(string path)
    {
        var content = readFile(path);
        return Result.Ok<object, string>(content);
    }

    /// <param name="x">Parameter of type int</param>
    /// <param name="y">Parameter of type int</param>
    /// <returns>Returns int</returns>
    public static int calculate(int x, int y)
    {
        return x + y * 2;
    }
}