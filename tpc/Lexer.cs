namespace TPC;

internal class Lexer
{
    // Lexer, returning tokens, including peeking.

    // define(["utils", "Token", "PascalError"], function (utils, Token, PascalError) {
    // Whether to print tokens as they're read.
    bool PRINT_TOKENS = false;
    private Stream stream;
    private Token nextToken;

    public Lexer(Stream stream)
    {
        this.stream = stream;
        this.nextToken = null;

        for (var i = 0; i < RESERVED_WORDS.Length; i++)
        {
            RESERVED_WORDS_MAP[RESERVED_WORDS[i]] = true;
        }
    }

    // All valid symbols.
    string[] SYMBOLS = ["<", "<>", "<<", ":", ":=", ">", ">>", "<=", ">=", "-", "+",
            "*", "/", ";", ",", "[", "]", "(", ")", "=", "^", "@", "(*" ];

    // All reserved words.
    string[] RESERVED_WORDS = ["program", "var", "begin", "end", "type", "procedure", "function",
            "uses", "for", "while", "repeat", "do", "then", "if", "else", "to", "downto", "until",
            "array", "of", "not", "record", "or", "and", "div", "mod", "const", "exit" ];
    readonly Dictionary<string, bool> RESERVED_WORDS_MAP = [];
    

    public bool IsReservedWord(string value)
    {
        //TODO: MVM  return RESERVED_WORDS_MAP.hasOwnProperty(value.toLowerCase());
        return RESERVED_WORDS_MAP[value.ToLower()];
    }

    // Returns the next token.
    public Token Next()
    {
        var token = this.Peek();

        // We've used up this token, force the next next() or peek() to fetch another.
        this.nextToken = null;

        return token;
    }

    // Peeks at the next token.
    public Token Peek()
    {
        // Fetch another token if necessary.
        if (this.nextToken == null)
        {
            this.nextToken = this.FetchNextToken();
        }

        return this.nextToken;
    }

    // Always gets another token.
    public Token FetchNextToken()
    {
        var ch = '\0';  //TODO: MVM
        var lineNumber = -1; //TODO: MVM

        // Skip whitespace.
        do
        {
            // Keep this updated as we walk through the whitespace.
            lineNumber = this.stream.lineNumber;

            ch = this.stream.Next();
            if (ch == -1)
            {
                return new Token(null, Token.EOF);
            }
        } while (Utils.IsWhitespace(ch));

        // Check each type of token.
        var token = this.PickLongestToken(ch, SYMBOLS);
        if (token != null && token.IsSymbol("(*"))
        {
            // Comment.

            // Keep reading until we get "*)".
            var value = "";
            while (true)
            {
                ch = this.stream.Next();
                if (ch == -1)
                {
                    break;
                }
                else if (ch == '*' && this.stream.Peek() == ')')
                {
                    // Skip ")".
                    this.stream.Next();
                    break;
                }
                value += ch;
            }
            token = new Token(value, Token.COMMENT);
        }
        if (token == null && Utils.IsIdentifierStart(ch))
        {
            // Keep adding more characters until we're not part of this token anymore.
            var value = "";
            while (true)
            {
                value += ch;
                ch = this.stream.Peek();
                if (ch == -1 || !Utils.IsIdentifierPart(ch))
                {
                    break;
                }
                this.stream.Next();
            }
            var tokenType = IsReservedWord(value) ? Token.RESERVED_WORD : Token.IDENTIFIER;
            token = new Token(value, tokenType);
        }
        if (token == null && (Utils.IsDigit(ch) || ch == '.'))
        {
            if (ch == '.')
            {
                // This could be a number, a dot, or two dots.
                var nextCh = this.stream.Peek();
                if (nextCh == '.')
                {
                    // Two dots.
                    this.stream.Next();
                    token = new Token("..", Token.SYMBOL);
                }
                else if (!Utils.IsDigit(nextCh))
                {
                    // Single dot.
                    token = new Token(".", Token.SYMBOL);
                }
                else
                {
                    // It's a number, leave token null.
                }
            }
            if (token == null)
            {
                // Parse number. Keep adding more characters until we're not
                // part of this token anymore.
                var value = "";
                var sawDecimalPoint = ch == '.';
                var sawExp = false;
                var justSawExp = false;
                while (true)
                {
                    value += ch;
                    ch = this.stream.Peek();
                    if (ch == -1)
                    {
                        break;
                    }
                    if (ch == '.' && !sawExp)
                    {
                        // This may be a decimal point, but it may be the start
                        // of a ".." symbol. Peek twice and push back.
                        this.stream.Next();
                        var nextCh = this.stream.Peek();
                        this.stream.PushBack(ch);
                        if (nextCh == '.')
                        {
                            // Double dot, end of number.
                            break;
                        }

                        // Now see if this single point is part of us or a separate symbol.
                        if (sawDecimalPoint)
                        {
                            break;
                        }
                        else
                        {
                            // Allow one decimal point.
                            sawDecimalPoint = true;
                        }
                    }
                    else if (ch.ToString().ToLower() == "e" && !sawExp)
                    {
                        // Start exponential section.
                        sawExp = true;
                        justSawExp = true;
                    }
                    else if (justSawExp)
                    {
                        if (ch == '+' || ch == '-' || Utils.IsDigit(ch))
                        {
                            // All good, this is required after "e".
                            justSawExp = false;
                        }
                        else
                        {
                            // Not allowed after "e".
                            token = new Token(value + ch, Token.NUMBER);
                            token.lineNumber = lineNumber;
                            throw new PascalError(token, "Unexpected character \"" + ch +
                                            "\" while reading exponential form");
                        }
                    }
                    else if (!Utils.IsDigit(ch))
                    {
                        break;
                    }
                    this.stream.Next();
                }
                token = new Token(value, Token.NUMBER);
            }
        }
        if (token == null && ch == '{')
        {
            // Comment.

            // Skip opening brace.
            ch = this.stream.Next();

            // Keep adding more characters until we're not part of this token anymore.
            var value = "";
            while (true)
            {
                value += ch;
                ch = this.stream.Next();
                if (ch == -1 || ch == '}')
                {
                    break;
                }
            }
            token = new Token(value, Token.COMMENT);
        }
        if (token == null && ch == '\'')
        {
            // String literal.

            // Skip opening quote.
            ch = this.stream.Next();

            // Keep adding more characters until we're not part of this token anymore.
            var value = "";
            while (true)
            {
                value += ch;
                ch = this.stream.Next();
                if (ch == '\'')
                {
                    // Handle double quotes.
                    if (this.stream.Peek() == '\'')
                    {
                        // Eat next quote. First one will be added at top of loop.
                        this.stream.Next();
                    }
                    else
                    {
                        break;
                    }
                }
                else if (ch == -1)
                {
                    break;
                }
            }
            token = new Token(value, Token.STRING);
        }
        if (token == null)
        {
            // Unknown token.
            token = new Token(ch.ToString(), Token.SYMBOL);
            token.lineNumber = lineNumber;
            throw new PascalError(token, "unknown symbol");
        }
        token.lineNumber = lineNumber;

        if (PRINT_TOKENS)
        {
            Console.WriteLine("Fetched token \"" + token.value + "\" of type " +
                        token.tokenType + " on line " + token.lineNumber);
        }

        return token;
    }

    // Find the longest symbols in the specified list. Returns a Token or null.
   public Token PickLongestToken(char ch,string[] symbols)
    {
        string longestSymbol = null;
        var nextCh = this.stream.Peek();
        var twoCh = nextCh == -1 ? ch : ch + nextCh;

        for (var i = 0; i < symbols.Length; i++)
        {
            var symbol = symbols[i];

            if ((symbol.Length == 1 && ch.ToString() == symbol) ||
                (symbol.Length == 2 && twoCh.ToString() == symbol))
            {

                if (longestSymbol == null || symbol.Length > longestSymbol.Length)
                {
                    longestSymbol = symbol;
                }
            }
        }

        if (longestSymbol == null)
        {
            return null;
        }

        if (longestSymbol.Length == 2)
        {
            // Eat the second character.
            this.stream.Next();
        }

        return new Token(longestSymbol, Token.SYMBOL);
    }
}