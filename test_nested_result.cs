using System;

public class Result<T, E>
{
    public readonly bool IsSuccess;
    public readonly T Value;
    public readonly E Error;
    
    public bool IsError
    {
        get { return !IsSuccess; }
    }

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
}

public class Test
{
    // This is what I'm generating - and it should be correct
    public static Result<Result<int, string>, bool> TestCorrect()
    {
        return Result.Ok<Result<int, string>, bool>(Result.Ok<int, string>(42));
    }
    
    // This is what the test expects - and it should be wrong
    public static Result<Result<int, string>, bool> TestExpected()
    {
        return Result.Ok<object, bool>(Result.Ok<int, string>(42));
    }
}