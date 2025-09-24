namespace TPC;

// A table of symbols, where each symbol can be a variable, procedure, or
// function. A table is for a lexical scope only, but has a link to the
// lexical scope it's nested in.
internal class SymbolTable
{
    internal int totalVariableSize;
    internal int totalParameterSize;
    private int totalTypedConstantsSize;
    internal Dictionary<string, Symbol> symbols;
    private static Dictionary<string, Symbol> types;
    internal SymbolTable parentSymbolTable;
    internal Native native;

    // The parent symbol table must be lexical, not dynamic.
    public SymbolTable(SymbolTable parentSymbolTable)
    {
        // Map from symbol name (all lowercase, since Pascal is case-insensitive) to
        // a Symbol object. This stores variables, constants, procedure, and functions.
        // Basically any symbol that can be references in an expression.
        this.symbols = [];

        // Map from type name (all lowercase, since Pascal is case-insensitive) to
        // a Symbol object. This stores user-defined types.
        types = [];

        // Parent of this table. Symbols not found in this table are looked up in the
        // parent one if it's not null.
        this.parentSymbolTable = parentSymbolTable;

        // Registry of native functions. We only have one of these, so if we have a parent,
        // use its object.
        this.native = parentSymbolTable != null ? parentSymbolTable.native : new Native();

        // Size (in words) of all variables in this frame.
        this.totalVariableSize = 0;

        // Size (in words) of all parameters in this frame.
        this.totalParameterSize = 0;

        // Size (in words) of all typed constants in this frame.
        this.totalTypedConstantsSize = 0;
    }

    // Adds a symbol to the table. Returns the Symbol object.
    public Symbol AddSymbol(string name, int nodeType, Node type, bool byReference)
    {
        var address = -1; // Indicates error.

        // Default to false.
        byReference = byReference || false;

        switch (nodeType)
        {
            case Node.VAR:
                // For this to work, all parameters must be added to the symbol table
                // before any variable is added.
                address = Inst.defs.MARK_SIZE + totalParameterSize + totalVariableSize;
                totalVariableSize += (int)type.GetTypeSize();
                break;
            case Node.CONST:
                break;
            case Node.TYPED_CONST:
                // They end up being copied to the stack at the start of
                // a function call, like a regular variable.
                address = Inst.defs.MARK_SIZE + totalParameterSize + totalVariableSize;
                totalVariableSize += (int)type.GetTypeSize();
                break;
            case Node.PARAMETER:
                address = Inst.defs.MARK_SIZE + totalParameterSize;
                totalParameterSize += byReference == true ? 1 : (int)type.GetTypeSize();
                break;
        }

        var symbol = new Symbol(name, type, address, byReference);
        this.symbols[name.ToLower()] = symbol;

        return symbol;
    }

    // Add a user-defined type, returning the Symbol object.
    public Symbol AddType(string name, Node type)
    {
        var symbol = new Symbol(name, type, 0, false);
        types[name.ToLower()] = symbol;

        return symbol;
    }

    // Returns the SymbolLookup object for the name. If the name is not found
    // in this table, the parent table is consulted if it's set. Throws if not
    // found. The nodeType is optional. If set, only nodes of that type will
    // be returned. The "level" parameter is for internal use and should be left out.
    public SymbolLookup GetSymbol(Token token, int? nodeType = null, int? level = null)
    {
        var name = token.value.ToLower();

        // Default to zero.
        // level = level || 0;

        if (this.symbols.ContainsKey(name))
        {
            var symbol = this.symbols[name];

            // Match optional nodeType.
            if (nodeType != null || symbol.type.nodeType == nodeType)
            {
                return new SymbolLookup(symbol, level.Value);
            }
        }

        if (this.parentSymbolTable != null)
        {
            return this.parentSymbolTable.GetSymbol(token, nodeType, level + 1);
        }

        throw new PascalError(token, "can't find symbol");
    }

    // Returns a SymbolLookup object for the type name. If the name is not
    // found in this table, the parent table is consulted if it's set. Throws
    // if not found. The "level" parameter is for internal use and should be left out.
    public SymbolLookup GetType(Token token, int level = 0) //TODO: MVM check level default
    {
        var name = token.value.ToLower();

        // Default to zero.
        //level = level || 0;

        if (types.ContainsKey(name))
        {
            var symbol = types[name];
            return new SymbolLookup(symbol, level);
        }

        if (this.parentSymbolTable != null)
        {
            return this.parentSymbolTable.GetType(token, level + 1);
        }

        throw new PascalError(token, "unknown type");
    }

    // Add a native constant to the symbol table.
    public void AddNativeConstant(string name, object value, Node type)
    {
        Node valueNode = type.GetSimpleTypeCode() switch
        {
            Inst.defs.A => Node.MakePointerNode(value),
            Inst.defs.B => Node.MakeBooleanNode(value.ToString()),
            _ => Node.MakeNumberNode(value.ToString()),
        };
        valueNode.expressionType = type;

        var symbol = this.AddSymbol(name, Node.CONST, type, false);
        symbol.value = valueNode;
    }

    // Add a native function to the symbol table.
    public Symbol AddNativeFunction(string name, Node returnType, List<Node> parameterTypes, object fn)
    {
        // Add to table of builtins first (for CSP call).
        var nativeProcedure = new NativeProcedure(name, returnType, parameterTypes, fn);
        var index = this.native.Add(nativeProcedure);


        //// Function that takes a type and an index and returns a PARAMETER for it.
        static Node MakeParameter(object type, int index)
        {
            var name = Node.MakeIdentifierNode(((char)(97 + index)).ToString()); // "a", "b", ...
            return new Node(Node.PARAMETER, null,
                    new Dictionary<string, object> { 
                        { "name", name },
                        { "type", type },
                     });
        }


        
        //// Make function type.

        var type = new Node(Node.SUBPROGRAM_TYPE, null, new Dictionary<string, object> {

            { "parameters", new Dictionary<object, object> { { parameterTypes, MakeParameter } } },
            { "returnType", returnType}
                            });

        // Add to this symbol table.
        var symbol = this.AddSymbol(name, Node.SUBPROGRAM_TYPE, type, false);


        // Remember the native index.
        symbol.address = index;

        // Mark it as native.
        symbol.isNative = true;

        return symbol;
    }

    // Add a native type (such as "integer") to the symbol table.
    public void AddNativeType(string name, Node type)
    {
        // Nothing special here, it's just like a user-defined type.
        AddType(name, type);
    }

    // Create a default symbol table with all built-in symbols.
    public static SymbolTable MakeBuiltinSymbolTable()
    {
        var symbolTable = new SymbolTable(null);

        Modules.ImportModule("__builtin__", symbolTable);

        return symbolTable;
    }

}