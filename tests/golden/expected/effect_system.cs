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
/// Effects: Database, Logging
/// </summary>
/// <param name="name">Parameter of type string</param>
/// <param name="email">Parameter of type string</param>
/// <returns>Returns Result<int, string></returns>
public static Result<int, string> saveUser(string name, string email)
{
    var userId_result = generateId();
    if (userId_result.IsError)
        return userId_result;
    var userId = userId_result.Value;
    return Result.Ok(userId);
}

/// <summary>
/// Effects: Network, Logging
/// </summary>
/// <param name="url">Parameter of type string</param>
/// <returns>Returns Result<string, string></returns>
public static Result<string, string> fetchData(string url)
{
    return Result.Ok("data");
}

/// <summary>
/// Effects: FileSystem, Logging
/// </summary>
/// <param name="path">Parameter of type string</param>
/// <returns>Returns Result<string, string></returns>
public static Result<string, string> processFile(string path)
{
    var content_result = readFile(path);
    if (content_result.IsError)
        return content_result;
    var content = content_result.Value;
    return Result.Ok(content);
}

/// <summary>
/// Pure function - no side effects
/// </summary>
/// <param name="x">Parameter of type int</param>
/// <param name="y">Parameter of type int</param>
/// <returns>Returns int</returns>
public static int calculate(int x, int y)
{
    return x + y * 2;
}