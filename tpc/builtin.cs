namespace TPC;

// Builtin symbols, such as "Sin()" and "Pi".

// Special handling of Random() because its return type depends on whether
// it has an argument.
internal class Builtin
{

    // Import all the symbols for the builtins.
    public static void ImportSymbols(SymbolTable symbolTable)
    {
        // Built-in types.
        symbolTable.AddNativeType("String", Node.stringType);
        symbolTable.AddNativeType("Integer", Node.integerType);
        symbolTable.AddNativeType("ShortInt", Node.integerType);
        symbolTable.AddNativeType("LongInt", Node.integerType);
        symbolTable.AddNativeType("Char", Node.charType);
        symbolTable.AddNativeType("Boolean", Node.booleanType);
        symbolTable.AddNativeType("Real", Node.realType);
        symbolTable.AddNativeType("Double", Node.realType);
        symbolTable.AddNativeType("Pointer", Node.pointerType);

        // Constants and functions.
        symbolTable.AddNativeConstant("Nil", null,
            new Node(Node.SIMPLE_TYPE, new Token("Nil", Token.IDENTIFIER),
            new Dictionary<string, object> {
                        { "typeCode",  Inst.defs.A },
                        { "typeName", null },  // Important -- this is what makes this nil.
                        { "type", null }
            }));
        symbolTable.AddNativeConstant("True", true, Node.booleanType);
        symbolTable.AddNativeConstant("False", false, Node.booleanType);
        symbolTable.AddNativeConstant("Pi", Math.PI, Node.realType);


        //TODO: MVM
        symbolTable.AddNativeFunction("Sin", Node.realType, new List<Node>() { Node.realType },
                   (double t) => { return Math.Sin(t); });
        symbolTable.AddNativeFunction("Cos", Node.realType, new List<Node>() { Node.realType },
                   (double t) => { return Math.Cos(t); });
        symbolTable.AddNativeFunction("Round", Node.integerType, new List<Node>() { Node.realType },
                   (double t) => { return Math.Round(t); });
        symbolTable.AddNativeFunction("Trunc", Node.integerType, new List<Node>() { Node.realType },
                     (double t) => { return (t < 0) ? Math.Ceiling(t) : Math.Floor(t); });
        symbolTable.AddNativeFunction("Odd", Node.booleanType, new List<Node>() { Node.realType },
                    (double t) => { return Math.Round(t) % 2 != 0; });
        symbolTable.AddNativeFunction("Abs", Node.realType, new List<Node>() { Node.realType },
                     (double t) => { return Math.Abs(t); });
        symbolTable.AddNativeFunction("Sqrt", Node.realType, new List<Node>() { Node.realType },
                     (double t) => { return Math.Sqrt(t); });
        symbolTable.AddNativeFunction("Ln", Node.realType, new List<Node>() { Node.realType },
                    (double t) => { return Math.Log(t); });
        symbolTable.AddNativeFunction("Sqr", Node.realType, new List<Node>() { Node.realType },
                     (double t) => { return t * t; });
        symbolTable.AddNativeFunction("Random", Node.realType, new List<Node>(),
            (double t) =>
            {
                Random rand = new Random();
                return t == null ? rand.NextDouble() : Math.Round(rand.NextDouble() * t);
            });
        symbolTable.AddNativeFunction("Randomize", Node.voidType, new List<Node>(),
                    () => { /* Nothing. */ });
        var symbol = symbolTable.AddNativeFunction("Inc", Node.voidType,

            [Node.integerType, Node.integerType], (Machine.Control ctl, int v, int? dv) =>
            {

                if (dv == null)
                {
                    dv = 1;
                }
                ctl.writeDstore(v, ctl.readDstore(v) + dv.Value);
            });
        symbol.type.parameters[0].byReference = true;
        symbolTable.AddNativeFunction("WriteLn", Node.voidType, new List<Node>(), (Machine.Control ctl) =>
        {
            // Skip ctl parameter.
            var elements = new List<string>();
            //TODO: MVM
            //for (var i = 1; i < arguments.length; i++)
            //{
            //    // Convert to string.
            //    elements.Add("" + arguments[i]);
            //}
            ctl.writeln(string.Join(" ", elements.ToArray()));
        });
        symbolTable.AddNativeFunction("ReadLn", Node.stringType, new List<Node>(), (Machine.Control ctl) =>
        {
            // Suspend the machine so that the browser can get keys to us.
            ctl.suspend();

            // Ask the IDE to read a line for us.
            ctl.readln((string line) =>
            {
                ctl.push(Convert.ToInt32(line));
                ctl.resume();
            });

            // We're a function, so we should return something, but we've
            // suspended the machine, so it doesn't matter.
        });
        symbolTable.AddNativeFunction("Halt", Node.voidType, new List<Node>(), (Machine.Control ctl) =>
        // Halt VM.
           { ctl.stop(); });
        symbolTable.AddNativeFunction("Delay", Node.voidType, new List<Node>() { Node.integerType },
                                      (Machine.Control ctl, int ms) =>
                                      {
                                          // Tell VM to delay by ms asynchronously.
                                          ctl.delay(ms);
                                      });
        symbol = symbolTable.AddNativeFunction("New", Node.voidType,

                                     new List<Node>() { Node.pointerType, Node.integerType },
                                      (Machine.Control ctl, int p, int size) =>
                                      {

                                          // Allocate and store address in p.
                                          ctl.writeDstore(p, ctl.malloc(size));
                                      });
        symbol.type.parameters[0].byReference = true;
        symbol = symbolTable.AddNativeFunction("GetMem", Node.voidType,

                                      new List<Node>() { Node.pointerType, Node.integerType },
                                       (Machine.Control ctl, int p, int size) =>
                                       {
                                           // Allocate and store address in p.
                                           ctl.writeDstore(p, ctl.malloc(size));
                                       });
        symbol.type.parameters[0].byReference = true;
        symbol = symbolTable.AddNativeFunction("Dispose", Node.voidType,

                                     new List<Node>() { Node.pointerType },
                                        (Machine.Control ctl, int p) =>
                                        {
                                            // Free p and store 0 (nil) into it.
                                            ctl.free(ctl.readDstore(p));
                                            ctl.writeDstore(p, 0);
                                        });
        symbol.type.parameters[0].byReference = true;
    }
}
