using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cadenza.Tests.Manual;

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

public static class CadenzaProgram
{
    /// <param name="age">Parameter of type int</param>
    /// <returns>Returns Result<string, string></returns>
    public static Result<string, string> validate_age(int age)
    {
        if (!(age >= 0))
        {
            return Result.Error<string, string>("Age cannot be negative");
        }
        return Result.Ok<string, string>("Valid");
    }
}