using System;

namespace Cadenza.Golden.StringInterpolation
{

public static class CadenzaProgram
{
/// <summary>
/// </summary>
/// <param name="name">Parameter of type string</param>
/// <param name="age">Parameter of type int</param>
/// <returns>Returns string</returns>
public static string greet(string name, int age)
{
    return string.Format("Hello {0}, you are {1} years old!", name, age);
}

/// <summary>
/// </summary>
/// <param name="user">Parameter of type string</param>
/// <param name="count">Parameter of type int</param>
/// <param name="status">Parameter of type string</param>
/// <returns>Returns string</returns>
public static string formatMessage(string user, int count, string status)
{
    return string.Format("User {0} has {1} messages and status is {2}", user, count, status);
}

/// <summary>
/// Pure function - no side effects
/// </summary>
/// <param name="dir">Parameter of type string</param>
/// <param name="file">Parameter of type string</param>
/// <param name="ext">Parameter of type string</param>
/// <returns>Returns string</returns>
public static string createPath(string dir, string file, string ext)
{
    return string.Format("{0}/{1}.{2}", dir, file, ext);
}

}

}