namespace TPC;

public class Token(string value, int tokenType)
{
    // Token types.
    public const int IDENTIFIER = 0;
    public const int NUMBER = 1;
    public const int SYMBOL = 2;
    public const int COMMENT = 3;
    public const int STRING = 4;
    public const int EOF = 5;
    public const int RESERVED_WORD = 6;
    public string value = value;
    public int tokenType = tokenType;
    public int lineNumber = -1;

    // Returns whether this token is a reserved word, such as "for". These are
    // case-insensitive.
    public bool IsReservedWord(string reservedWord)
    {
        return this.tokenType == Token.RESERVED_WORD &&
            this.value.ToLower() == reservedWord.ToLower();
    }

    // Returns whether this token is equal to the specified token. The line
    // number is not taken into account; only the type and value.
    public bool IsEqualTo(Token other)
    {
        return this.tokenType == other.tokenType && this.value == other.value;
    }

    // Returns whether this is the specified symbol.
    public bool IsSymbol(string symbol)
    {
        return this.tokenType == Token.SYMBOL && this.value == symbol;
    }

}
