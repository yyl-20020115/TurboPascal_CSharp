namespace TPC;

// Exception for parse, compile, and runtime errors.
public class PascalError(Token token, string message) : Exception
{
    public Token token = token;
    public string message = message;

    public string GetMessage()
    {
        var message = "Error: " + this.message;

        // Add token info.
        if (this.token != null)
        {
            message += " (\"" + this.token.value + "\", line " + this.token.lineNumber + ")";
        }

        return message;
    }
}
