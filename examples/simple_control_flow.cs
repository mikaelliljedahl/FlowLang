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

public static bool test_boolean(int a, int b)
{
    return a > 0 && b > 0;
}

public static Result<string, string> test_guard(int value)
{
    if (!(value >= 0))
    {
        return Result.Error("Negative value");
    }

    return Result.Ok("Valid");
}

public static string test_if(int age)
{
    if (age < 18)
    {
        return "Minor";
    }
    else
    {
        return "Adult";
    }
}