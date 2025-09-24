namespace TPC;

// The result of a symbol lookup.
internal class SymbolLookup(Symbol symbol, int level)
{
    public Symbol symbol = symbol;
    public int level = level;
}
