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

public static Result<string, string> process_user_data(string name, int age, string email)
{
    if (!(name != ""))
    {
        return Result.Error("Name cannot be empty");
    }

    if (!(age >= 0 && age <= 150))
    {
        return Result.Error("Age must be between 0 and 150");
    }

    if (!(email != ""))
    {
        return Result.Error("Email cannot be empty");
    }

    if (age < 13)
    {
        return Result.Ok("Child user registered");
    }
    else
    {
        if (age >= 13 && age < 18)
        {
            return Result.Ok("Teen user registered");
        }
        else
        {
            if (age >= 18 && age < 65)
            {
                return Result.Ok("Adult user registered");
            }
            else
            {
                return Result.Ok("Senior user registered");
            }
        }
    }
}

public static bool access_control(int user_level, bool is_admin, int resource_level, bool is_owner)
{
    return is_admin || is_owner && user_level >= resource_level || is_admin;
}

public static bool validate_password_strength(string password, int min_length)
{
    if (!(password != ""))
    {
        return false;
    }

    return !password == "" && !min_length > 0 && password == "";
}