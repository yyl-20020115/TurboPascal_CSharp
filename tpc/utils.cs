namespace TPC;

// Utility functions.
public static class Utils
{

    // Whether the character is alphabetic.
    public static bool IsAlpha(char ch) => (ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z');

    // Whether the character is a digit.
    public static bool IsDigit(char ch) => ch >= '0' && ch <= '9';

    // Whether the character is a valid first character of an identifier.
    public static bool IsIdentifierStart(char ch) => IsAlpha(ch) || ch == '_';

    // Whether the character is a valid subsequent (non-first) character of an identifier.
    public static bool IsIdentifierPart(char ch) => IsIdentifierStart(ch) || IsDigit(ch);

    // Whether the character is whitespace.
    public static bool IsWhitespace(char ch) => ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r';

    // Format number or string to width characters, left-aligned.
    public static string LeftAlign(string value, int width)
    {
        // Convert to string.
        value = "" + value;

        // Pad to width.
        while (value.Length < width)
        {
            value = value + " ";
        }

        return value;
    }

    // Format number or string to width characters, right-aligned.
    public static string RightAlign(string value, int width)
    {
        // Convert to string.
        value = "" + value;

        // Pad to width.
        while (value.Length < width)
        {
            value = " " + value;
        }

        return value;
    }

    // Truncate toward zero.
    public static decimal Trunc(int value) => value < 0 ? Math.Ceiling((decimal)value) : Math.Floor((decimal)value);

    // Repeat a string "count" times.
    public static string RepeatString(string s, int count)
    {
        var result = "";

        // We go through each bit of "count", adding a string of the right length
        // to "result" if the bit is 1.
        while (true)
        {
            if ((count & 1) != 0)
            {
                result += s;
            }

            // Move to the next bit.
            count >>= 1;
            if (count == 0)
            {
                // Exit here before needlessly doubling the size of "s".
                break;
            }

            // Double the length of "s" to correspond to the value of the shifted bit.
            s += s;
        }

        return result;
    }

    // Log an object written out in human-readable JSON. This can't handle
    // circular structures.
    public static void LogAsJson(Object obj)
    {
        // console.log(JSON.stringify(obj, null, 2));
        Console.WriteLine(obj.ToString());
    }


}
