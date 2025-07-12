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
/// Pure function - no side effects
/// </summary>
/// <param name="amount">Parameter of type int</param>
/// <returns>Returns int</returns>
public static int calculate_tax(int amount)
{
    return amount * 8 / 100;
}

/// <summary>
/// Effects: Logging
/// </summary>
/// <param name="message">Parameter of type string</param>
/// <returns>Returns Result<string, string></returns>
public static Result<string, string> log_message(string message)
{
    return Result.Ok(message);
}

/// <summary>
/// Effects: Database, Logging
/// </summary>
/// <param name="user_id">Parameter of type int</param>
/// <param name="name">Parameter of type string</param>
/// <returns>Returns Result<int, string></returns>
public static Result<int, string> save_user(int user_id, string name)
{
    var log_result_result = log_message("Saving user: " + name);
    if (log_result_result.IsError)
        return log_result_result;
    var log_result = log_result_result.Value;
    return Result.Ok(user_id);
}

/// <summary>
/// Effects: Database, Network, Logging, FileSystem
/// </summary>
/// <param name="user_id">Parameter of type int</param>
/// <returns>Returns Result<string, string></returns>
public static Result<string, string> fetch_user_data(int user_id)
{
    var log_result_result = log_message("Fetching user data for ID: " + user_id);
    if (log_result_result.IsError)
        return log_result_result;
    var log_result = log_result_result.Value;
    var user_name = "User" + user_id;
    var profile_data = "Profile data from API";
    var cached_data = "Cached user preferences";
    var final_result = user_name + " - " + profile_data + " - " + cached_data;
    return Result.Ok(final_result);
}

/// <summary>
/// Effects: Memory, IO
/// </summary>
/// <param name="size">Parameter of type int</param>
/// <returns>Returns Result<string, string></returns>
public static Result<string, string> process_large_dataset(int size)
{
    var buffer_size = size * 1024;
    var processed_data = "Processed " + buffer_size + " bytes";
    return Result.Ok(processed_data);
}

/// <summary>
/// Effects: Database, Network, Logging, FileSystem, Memory, IO
/// </summary>
/// <param name="user_id">Parameter of type int</param>
/// <param name="name">Parameter of type string</param>
/// <returns>Returns Result<string, string></returns>
public static Result<string, string> complete_user_workflow(int user_id, string name)
{
    var save_result_result = save_user(user_id, name);
    if (save_result_result.IsError)
        return save_result_result;
    var save_result = save_result_result.Value;
    var fetch_result_result = fetch_user_data(user_id);
    if (fetch_result_result.IsError)
        return fetch_result_result;
    var fetch_result = fetch_result_result.Value;
    var process_result_result = process_large_dataset(100);
    if (process_result_result.IsError)
        return process_result_result;
    var process_result = process_result_result.Value;
    var workflow_result = "Workflow completed for user: " + name;
    return Result.Ok(workflow_result);
}