public static bool complex_boolean(int a, int b, bool flag)
{
    return a > 0 && b > 0 || flag && a != b;
}

public static bool test_not(bool value)
{
    return !value;
}

public static string nested_if(int score)
{
    if (score >= 90)
    {
        return "A";
    }
    else
    {
        if (score >= 80)
        {
            return "B";
        }
        else
        {
            if (score >= 70)
            {
                return "C";
            }
            else
            {
                return "F";
            }
        }
    }
}