namespace TPC;

// A token filter that strips out comment tokens.
internal class CommentStripper(Lexer lexer)
{
    private readonly Lexer lexer = lexer;

    // Returns the next token.
    public Token Next()
    {
        while (true)
        {
            var token = this.lexer.Next();
            if (token.tokenType != Token.COMMENT)
            {
                return token;
            }
        }
    }

    // Peeks at the next token.
    public Token Peek()
    {
        while (true)
        {
            var token = this.lexer.Peek();
            if (token.tokenType != Token.COMMENT)
            {
                return token;
            }
            else
            {
                // Skip the comment.
                this.lexer.Next();
            }
        }
    }
}
