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
    /// <param name="x">Parameter of type int</param>
    /// <returns>Returns string</returns>
    public static string testIfElse(int x)
    {
        if (x > 10)
        {
            return "large";
        }
        else
        {
            if (x > 5)
            {
                return "medium";
            }
            else
            {
                return "small";
            }
        }
    }

    /// <param name="x">Parameter of type int</param>
    /// <returns>Returns int</returns>
    public static int testGuard(int x)
    {
        if (!(x > 0))
        {
            return 0;
        }

        return x * 2;
    }

    /// <param name="x">Parameter of type int</param>
    /// <param name="y">Parameter of type int</param>
    /// <returns>Returns string</returns>
    public static string testNestedControl(int x, int y)
    {
        if (x > 0)
        {
            if (!(y > 0))
            {
                return "x positive, y not positive";
            }

            return "both positive";
        }
        else
        {
            return "x not positive";
        }
    }
}