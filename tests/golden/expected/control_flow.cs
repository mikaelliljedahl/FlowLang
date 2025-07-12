/// <summary>
/// </summary>
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

/// <summary>
/// </summary>
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

/// <summary>
/// </summary>
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