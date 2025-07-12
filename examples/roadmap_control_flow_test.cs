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

public static bool validate_user(string name, int age, string email)
{
    return name != "" && age > 0 && age < 150 && email != "";
}

public static Result<int, string> safe_divide(int a, int b)
{
    if (!(a >= 0))
    {
        return Result.Error("First number must be non-negative");
    }

    if (!(b != 0))
    {
        return Result.Error("Cannot divide by zero");
    }

    return Result.Ok(a / b);
}

public static Result<string, string> classify_age(int age)
{
    if (!(age >= 0))
    {
        return Result.Error("Age cannot be negative");
    }

    if (age < 18)
    {
        return Result.Ok("Minor");
    }
    else
    {
        if (age < 65)
        {
            return Result.Ok("Adult");
        }
        else
        {
            return Result.Ok("Senior");
        }
    }
}

public static bool complex_condition(int a, int b, bool c)
{
    return a > 0 && b > 0 || c && a != b;
}

public static bool is_not_empty(string text)
{
    return !text == "";
}