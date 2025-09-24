namespace TPC;

// Stores information about a single symbol.

/**
 * Create a new symbol.
 *
 * name: name of symbol (original case is fine).
 * type: type of the symbol (Node.SIMPLE_TYPE, etc.).
 * address:
 *     if variable: address of symbol relative to mark pointer.
 *     if user procedure: address in istore.
 *     if system procedure: index into native array.
 * isNative: true if it's a native subprogram.
 * value: node of value if it's a constant.
 * byReference: whether this symbol is a reference or a value. This only applies
 *     to function/procedure parameters.
 */
internal class Symbol(string name, Node type, int address, bool byReference)
{
    public bool isNative = false;
    public Node value = null;
    public bool byReference = byReference;
    public string name = name;
    public Node type = type;
    public int address = address;
}
