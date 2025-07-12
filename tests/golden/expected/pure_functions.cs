/// <summary>
/// Pure function - no side effects
/// </summary>
/// <param name="x">Parameter of type int</param>
/// <returns>Returns int</returns>
public static int square(int x)
{
    return x * x;
}

/// <summary>
/// Pure function - no side effects
/// </summary>
/// <param name="a">Parameter of type int</param>
/// <param name="b">Parameter of type int</param>
/// <returns>Returns int</returns>
public static int max(int a, int b)
{
    if (a > b)
    {
        return a;
    }
    else
    {
        return b;
    }
}