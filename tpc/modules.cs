namespace TPC;
internal class Modules
{
    internal static void ImportModule(string name, SymbolTable symbolTable)
    {
        switch (name.ToLower())
        {
            case "__builtin__":
                Builtin.ImportSymbols(symbolTable);
                break;
            case "crt":
                CRT.ImportSymbols(symbolTable);
                break;
            case "dos":
                // I don't know what goes in here.
                break;
            case "graph":
                Graph.ImportSymbols(symbolTable);
                break;
            case "mouse":
                mouse.importSymbols(symbolTable);
                break;
            case "printer":
                // I don't know what goes in here.
                break;
            default:
                throw new PascalError(null, "unknown module " + name);
        }
    }
}