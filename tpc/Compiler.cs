namespace TPC;

// Compiler from parse tree to bytecode.
internal class Compiler
{
    private readonly Stack<int> exitInstructions = [];

    public Compiler()
    {
        // This is a stack of lists of addresses of unconditional jumps (UJP) instructions
        // that should go to the end of the function/procedure in an Exit statement.
        // Each outer element represents a nested function/procedure we're compiling.
        // The inner list is an unordered list of addresses to update when we get to
        // the end of the function/procedure and know its last address.
    }

    // Given a parse tree, return the bytecode object.
    public ByteCode Compile(Node root)
    {
        var bytecode = new ByteCode(root.symbolTable.native);

        // Start at the root and recurse.
        this.GenerateBytecode(bytecode, root, null);

        // Generate top-level calling code.
        bytecode.SetStartAddress();
        bytecode.Add(Inst.defs.MST, 0, 0, "start of program -----------------");
        bytecode.Add(Inst.defs.CUP, 0, root.symbol.address, "call main program");
        bytecode.Add(Inst.defs.STP, 0, 0, "program end");

        return bytecode;
    }

    // Adds the node to the bytecode.
    public void GenerateBytecode(ByteCode bytecode, Node node, SymbolTable symbolTable)
    {
        switch (node.nodeType)
        {
            case Node.IDENTIFIER:
                var name = node.token.value;
                var symbolLookup = node.symbolLookup;
                if (symbolLookup.symbol.byReference)
                {
                    // Symbol is by reference. Must get its address first.
                    bytecode.Add(Inst.defs.LVA, symbolLookup.level,
                                 symbolLookup.symbol.address, "address of " + name);
                    bytecode.Add(Inst.defs.LDI, symbolLookup.symbol.type.typeCode,
                                 0, "value of " + name);
                }
                else
                {
                    // Here we could call _generateAddressBytecode() followed by an inst.defs.LDI,
                    // but loading the value directly is more efficient.
                    if (symbolLookup.symbol.type.nodeType == Node.SIMPLE_TYPE)
                    {
                        var opcode = symbolLookup.symbol.type.typeCode switch
                        {
                            Inst.defs.A => Inst.defs.LVA,
                            Inst.defs.B => Inst.defs.LVB,
                            Inst.defs.C => Inst.defs.LVC,
                            Inst.defs.I => Inst.defs.LVI,
                            Inst.defs.R => Inst.defs.LVR,
                            Inst.defs.S => Inst.defs.LVC,// A string is not a character, but there's no opcode
                                                         // for loading a string. Re-use LVC.
                            _ => throw new PascalError(node.token, "can't make code to get " +
                                                                                 symbolLookup.symbol.type.Print()),
                        };
                        bytecode.Add(opcode, symbolLookup.level,
                                     symbolLookup.symbol.address, "value of " + name);
                    }
                    else
                    {
                        // This is a more complex type, and apparently it's being
                        // passed by value, so we push the entire thing onto the stack.
                        var size = symbolLookup.symbol.type.GetTypeSize();
                        // For large parameters it would be more
                        // space-efficient (but slower) to have a loop.
                        for (var i = 0; i < size; i++)
                        {
                            bytecode.Add(Inst.defs.LVI, symbolLookup.level,
                                         symbolLookup.symbol.address + i,
                                         "value of " + name + " at index " + i);
                        }
                    }
                }
                break;
            case Node.NUMBER:
                var vNumber = (int)node.GetNumber();
                var cindex = bytecode.AddConstant(vNumber);

                // See if we're an integer or real.
                int typeCodeNumber;
                typeCodeNumber = (vNumber | 0) == vNumber ? Inst.defs.I : Inst.defs.R;

                bytecode.Add(Inst.defs.LDC, typeCodeNumber, cindex, "constant value " + vNumber);
                break;
            case Node.STRING:
                var vString = node.token.value;
                var cindexString = bytecode.AddConstant(vString);
                bytecode.Add(Inst.defs.LDC, Inst.defs.S, cindexString, "string '" + vString + "'");
                break;
            case Node.BOOLEAN:
                var vBoolean = node.token.value;
                bytecode.Add(Inst.defs.LDC, Inst.defs.B, node.GetBoolean() ? 1 : 0, "boolean " + vBoolean);
                break;
            case Node.POINTER:
                // This can only be nil.
                var cindexPointer = bytecode.AddConstant(0);
                bytecode.Add(Inst.defs.LDC, Inst.defs.A, cindexPointer, "nil pointer");
                break;
            case Node.PROGRAM:
            case Node.PROCEDURE:
            case Node.FUNCTION:
                var isFunction = node.nodeType == Node.FUNCTION;
                var nameFunction = node.name.token.value;

                // Begin a new frame for exit statements.
                this.BeginExitFrame();

                // Generate each procedure and function.
                for (var i = 0; i < node.declarations.Length; i++)
                {
                    var declaration = node.declarations[i];
                    if (declaration.nodeType == Node.PROCEDURE ||
                        declaration.nodeType == Node.FUNCTION)
                    {

                        this.GenerateBytecode(bytecode, declaration, node.symbolTable);
                    }
                }

                // Generate code for entry to block.
                node.symbol.address = bytecode.GetNextAddress();
                var frameSize = Inst.defs.MARK_SIZE + node.symbolTable.totalVariableSize +
                    node.symbolTable.totalParameterSize;
                bytecode.Add(Inst.defs.ENT, 0, frameSize, "start of " + nameFunction + " -----------------");

                // Generate code for typed constants.
                for (var i = 0; i < node.declarations.Length; i++)
                {
                    var declaration = node.declarations[i];
                    if (declaration.nodeType == Node.TYPED_CONST)
                    {
                        this.GenerateBytecode(bytecode, declaration, node.symbolTable);
                    }
                }

                // Generate code for block.
                this.GenerateBytecode(bytecode, node.block, node.symbolTable);

                // End the frame for exit statements.
                var ujpAddresses = this.EndExitFrame();
                var rtnAddress = bytecode.GetNextAddress();

                bytecode.Add(Inst.defs.RTN, isFunction ? node.expressionType.
                             returnType.GetSimpleTypeCode() : Inst.defs.P, 0, "end of " + nameFunction);

                // Update all of the UJP statements to point to RTN.
                for (var i = 0; i < ujpAddresses.Length; i++)
                {
                    bytecode.SetOperand2(ujpAddresses[i], rtnAddress);
                }
                break;
            case Node.USES:
            case Node.VAR:
            case Node.PARAMETER:
            case Node.CONST:
            case Node.ARRAY_TYPE:
            case Node.TYPE:
                // Nothing.
                break;
            case Node.BLOCK:
                for (var i = 0; i < node.statements.Length; i++)
                {
                    this.GenerateBytecode(bytecode, node.statements[i], symbolTable);
                }
                break;
            case Node.CAST:
                this.GenerateBytecode(bytecode, node.expression, symbolTable);
                var fromType = node.expression.expressionType;
                var toType = node.type;
                if (fromType.IsSimpleType(Inst.defs.I) && toType.IsSimpleType(Inst.defs.R))
                {
                    bytecode.Add(Inst.defs.FLT, 0, 0, "cast to float");
                }
                else
                {
                    throw new PascalError(node.token, "don't know how to compile a cast from " +
                                         fromType.Print() + " to " + toType.Print());
                }
                break;
            case Node.ASSIGNMENT:
                // Push address of LHS onto stack.
                this.GenerateAddressBytecode(bytecode, node.lhs, symbolTable);

                // Push RHS onto stack.
                this.GenerateBytecode(bytecode, node.rhs, symbolTable);

                // We don't look at the type code when executing, but might as
                // well set it anyway.
                var storeTypeCode = node.rhs.expressionType.GetSimpleTypeCode();

                bytecode.Add(Inst.defs.STI, storeTypeCode, 0, "store into " + node.lhs.Print());
                break;
            case Node.PROCEDURE_CALL:
            case Node.FUNCTION_CALL:
                var isFunctionFC = node.nodeType == Node.FUNCTION_CALL;
                var declType = isFunctionFC ? "function" : "procedure";
                var symbolLookupFC = node.name.symbolLookup;
                var symbol = symbolLookupFC.symbol;

                if (!symbol.isNative)
                {
                    bytecode.Add(Inst.defs.MST, symbolLookupFC.level, 0, "set up mark for " + declType);
                }

                // Push arguments.
                for (var i = 0; i < node.argumentList.Length; i++)
                {
                    var argument = node.argumentList[i];
                    if (argument.byReference)
                    {
                        this.GenerateAddressBytecode(bytecode, argument, symbolTable);
                    }
                    else
                    {
                        this.GenerateBytecode(bytecode, argument, symbolTable);
                    }
                }

                // See if this is a user procedure/function or native procedure/function.
                if (symbol.isNative)
                {
                    // The CSP index is stored in the address field.
                    var index = symbol.address;
                    bytecode.Add(Inst.defs.CSP, node.argumentList.Length, index,
                                 "call system " + declType + " " + symbol.name);
                }
                else
                {
                    // Call procedure/function.
                    var parameterSize = symbol.type.GetTotalParameterSize();
                    bytecode.Add(Inst.defs.CUP, parameterSize, symbol.address,
                                 "call " + node.name.Print());
                }
                break;
            case Node.REPEAT:
                var topOfLoop = bytecode.GetNextAddress();
                bytecode.AddComment(topOfLoop, "top of repeat loop");
                this.GenerateBytecode(bytecode, node.block, symbolTable);
                this.GenerateBytecode(bytecode, node.expression, symbolTable);
                bytecode.Add(Inst.defs.FJP, 0, topOfLoop, "jump to top of repeat");
                break;
            case Node.FOR:
                // Assign start value.
                var varNode = node.variable;
                this.GenerateAddressBytecode(bytecode, varNode, symbolTable);
                this.GenerateBytecode(bytecode, node.fromExpr, symbolTable);
                bytecode.Add(Inst.defs.STI, 0, 0, "store into " + varNode.Print());

                // Comparison.
                var topOfLoopFOR = bytecode.GetNextAddress();
                this.GenerateBytecode(bytecode, varNode, symbolTable);
                this.GenerateBytecode(bytecode, node.toExpr, symbolTable);
                bytecode.Add(node.downto ? Inst.defs.LES : Inst.defs.GRT,
                             Inst.defs.I, 0, "see if we're done with the loop");
                var jumpInstruction = bytecode.GetNextAddress();
                bytecode.Add(Inst.defs.TJP, 0, 0, "yes, jump to end");

                // Body.
                this.GenerateBytecode(bytecode, node.body, symbolTable);

                // Increment/decrement variable.
                this.GenerateAddressBytecode(bytecode, varNode, symbolTable);
                this.GenerateBytecode(bytecode, varNode, symbolTable);
                if (node.downto)
                {
                    bytecode.Add(Inst.defs.DEC, Inst.defs.I, 0, "decrement loop variable");
                }
                else
                {
                    bytecode.Add(Inst.defs.INC, Inst.defs.I, 0, "increment loop variable");
                }
                bytecode.Add(Inst.defs.STI, 0, 0, "store into " + varNode.Print());

                // Jump back to top.
                bytecode.Add(Inst.defs.UJP, 0, topOfLoopFOR, "jump to top of loop");

                var endOfLoop = bytecode.GetNextAddress();

                // Fix up earlier jump.
                bytecode.SetOperand2(jumpInstruction, endOfLoop);
                break;
            case Node.IF:
                var hasElse = node.elseStatement != null;

                // Do comparison.
                this.GenerateBytecode(bytecode, node.expression, symbolTable);
                var skipThenInstruction = bytecode.GetNextAddress();
                bytecode.Add(Inst.defs.FJP, 0, 0, "false, jump " + (hasElse ? "to else" : "past body"));

                // Then block.
                this.GenerateBytecode(bytecode, node.thenStatement, symbolTable);
                var skipElseInstruction = -1;
                if (hasElse)
                {
                    skipElseInstruction = bytecode.GetNextAddress();
                    bytecode.Add(Inst.defs.UJP, 0, 0, "jump past else");
                }

                // Else block.
                var falseAddress = bytecode.GetNextAddress();
                if (hasElse)
                {
                    this.GenerateBytecode(bytecode, node.elseStatement, symbolTable);
                }

                // Fix up earlier jumps.
                bytecode.SetOperand2(skipThenInstruction, falseAddress);
                if (hasElse != true)//TODO: MVM -1)
                {
                    var endOfIf = bytecode.GetNextAddress();
                    bytecode.SetOperand2(skipElseInstruction, endOfIf);
                }
                break;
            case Node.EXIT:
                // Return from procedure or function. We don't yet have the address
                // of the last instruction in this function, so we keep track of these
                // in an array and deal with them at the end.
                var address = bytecode.GetNextAddress();
                bytecode.Add(Inst.defs.UJP, 0, 0, "return from function/procedure");
                this.AddExitInstruction(address);
                break;
            case Node.WHILE:
                // Generate the expression test.
                var topOfLoopWHILE = bytecode.GetNextAddress();
                bytecode.AddComment(topOfLoopWHILE, "top of while loop");
                this.GenerateBytecode(bytecode, node.expression, symbolTable);

                // Jump over the statement if the expression was false.
                var jumpInstructionWHILE = bytecode.GetNextAddress();
                bytecode.Add(Inst.defs.FJP, 0, 0, "if false, exit while loop");

                // Generate the statement.
                this.GenerateBytecode(bytecode, node.statement, symbolTable);
                bytecode.Add(Inst.defs.UJP, 0, topOfLoopWHILE, "jump to top of while loop");

                // Fix up earlier jump.
                var endOfLoopWHILE = bytecode.GetNextAddress();
                bytecode.SetOperand2(jumpInstructionWHILE, endOfLoopWHILE);
                break;
            case Node.TYPED_CONST:
                // These are just initialized variables. Copy the values to their stack
                // location.
                var constAddress = bytecode.AddTypedConstants(node.rawData.data);

                for (var i = 0; i < node.rawData.length; i++)
                {
                    var typeCode = node.rawData.simpleTypeCodes[i];

                    bytecode.Add(Inst.defs.LDA, 0, node.symbol.address + i,
                                 "address of " + node.name.Print() +
                                 " on stack (element " + i + ")");
                    // It's absurd to create this many constants, one for each
                    // address in the const pool, but I don't see another
                    // straightforward way to do it. Creating an ad-hoc loop is
                    // hard because I don't know where I'd store the loop
                    // variable. Even if I could store it on the stack where we
                    // are, how would I pop it off at the end of the loop? We
                    // don't have a POP instruction.
                    var cindexTYPED_CONST = bytecode.AddConstant(constAddress + i);
                    bytecode.Add(Inst.defs.LDC, Inst.defs.A, cindexTYPED_CONST, "address of " +
                                 node.name.Print() + " in const area (element " + i + ")");
                    bytecode.Add(Inst.defs.LDI, typeCode, 0, "value of element");
                    bytecode.Add(Inst.defs.STI, typeCode, 0, "write value");
                }

                break;
            case Node.NOT:
                this.GenerateBytecode(bytecode, node.expression, symbolTable);
                bytecode.Add(Inst.defs.NOT, 0, 0, "logical not");
                break;
            case Node.NEGATIVE:
                this.GenerateBytecode(bytecode, node.expression, symbolTable);
                if (node.expression.expressionType.IsSimpleType(Inst.defs.R))
                {
                    bytecode.Add(Inst.defs.NGR, 0, 0, "real sign inversion");
                }
                else
                {
                    bytecode.Add(Inst.defs.NGI, 0, 0, "integer sign inversion");
                }
                break;
            case Node.ADDITION:
                this.GenerateNumericBinaryBytecode(bytecode, node, symbolTable,
                                                    "add", Inst.defs.ADI, Inst.defs.ADR);
                break;
            case Node.SUBTRACTION:
                this.GenerateNumericBinaryBytecode(bytecode, node, symbolTable,
                                                    "subtract", Inst.defs.SBI, Inst.defs.SBR);
                break;
            case Node.MULTIPLICATION:
                this.GenerateNumericBinaryBytecode(bytecode, node, symbolTable,
                                                    "multiply", Inst.defs.MPI, Inst.defs.MPR);
                break;
            case Node.DIVISION:
                this.GenerateNumericBinaryBytecode(bytecode, node, symbolTable,
                                                    "divide", null, Inst.defs.DVR);
                break;
            case Node.FIELD_DESIGNATOR:
                this.GenerateAddressBytecode(bytecode, node, symbolTable);
                bytecode.Add(Inst.defs.LDI, node.expressionType.GetSimpleTypeCode(), 0,
                             "load value of record field");
                break;
            case Node.ARRAY:
                // Array lookup.
                this.GenerateAddressBytecode(bytecode, node, symbolTable);
                bytecode.Add(Inst.defs.LDI, node.expressionType.GetSimpleTypeCode(), 0,
                             "load value of array element");
                break;
            case Node.ADDRESS_OF:
                this.GenerateAddressBytecode(bytecode, node.variable, symbolTable);
                break;
            case Node.DEREFERENCE:
                this.GenerateBytecode(bytecode, node.variable, symbolTable);
                bytecode.Add(Inst.defs.LDI, node.expressionType.GetSimpleTypeCode(), 0,
                             "load value pointed to by pointer");
                break;
            case Node.EQUALITY:
                this.GenerateComparisonBinaryBytecode(bytecode, node, symbolTable,
                                                       "equals", Inst.defs.EQU);
                break;
            case Node.INEQUALITY:
                this.GenerateComparisonBinaryBytecode(bytecode, node, symbolTable,
                                                       "not equals", Inst.defs.NEQ);
                break;
            case Node.LESS_THAN:
                this.GenerateComparisonBinaryBytecode(bytecode, node, symbolTable,
                                                       "less than", Inst.defs.LES);
                break;
            case Node.GREATER_THAN:
                this.GenerateComparisonBinaryBytecode(bytecode, node, symbolTable,
                                                       "greater than", Inst.defs.GRT);
                break;
            case Node.LESS_THAN_OR_EQUAL_TO:
                this.GenerateComparisonBinaryBytecode(bytecode, node, symbolTable,
                                                       "less than or equal to", Inst.defs.LEQ);
                break;
            case Node.GREATER_THAN_OR_EQUAL_TO:
                this.GenerateComparisonBinaryBytecode(bytecode, node, symbolTable,
                                                       "greater than or equal to", Inst.defs.GEQ);
                break;
            case Node.AND:
                this.GenerateComparisonBinaryBytecode(bytecode, node, symbolTable,
                                                       "and", Inst.defs.AND);
                break;
            case Node.OR:
                this.GenerateComparisonBinaryBytecode(bytecode, node, symbolTable,
                                                       "or", Inst.defs.IOR);
                break;
            case Node.INTEGER_DIVISION:
                this.GenerateNumericBinaryBytecode(bytecode, node, symbolTable,
                                                    "divide", Inst.defs.DVI, null);
                break;
            case Node.MOD:
                this.GenerateNumericBinaryBytecode(bytecode, node, symbolTable,
                                                    "mod", Inst.defs.MOD, null);
                break;
            default:
                throw new PascalError(null, "can't compile unknown node " + node.nodeType);
        }
    }

    // Generates code to do math on two operands.
    public void GenerateNumericBinaryBytecode(ByteCode bytecode, Node node, SymbolTable symbolTable, string opName, int? integerOpcode, int? realOpcode)
    {

        this.GenerateBytecode(bytecode, node.lhs, symbolTable);
        this.GenerateBytecode(bytecode, node.rhs, symbolTable);
        if (node.expressionType.nodeType == Node.SIMPLE_TYPE)
        {
            switch (node.expressionType.typeCode)
            {
                case Inst.defs.I:
                    if (integerOpcode == null)
                    {
                        throw new PascalError(node.token, "can't " + opName + " integers");
                    }
                    bytecode.Add(integerOpcode.Value, 0, 0, opName + " integers");
                    break;
                case Inst.defs.R:
                    if (realOpcode == null)
                    {
                        throw new PascalError(node.token, "can't " + opName + " reals");
                    }
                    bytecode.Add(realOpcode.Value, 0, 0, opName + " reals");
                    break;
                default:
                    throw new PascalError(node.token, "can't " + opName + " operands of type " +
                        Inst.defs.TypeCodeToName(node.expressionType.typeCode));
            }
        }
        else
        {
            throw new PascalError(node.token, "can't " + opName +
                                 " operands of type " + node.expressionType.Print());
        }
    }

    // Generates code to compare two operands.
    public void GenerateComparisonBinaryBytecode(ByteCode bytecode, Node node, SymbolTable symbolTable, string opName, int opcode)
    {

        this.GenerateBytecode(bytecode, node.lhs, symbolTable);
        this.GenerateBytecode(bytecode, node.rhs, symbolTable);
        var opType = node.lhs.expressionType;
        if (opType.nodeType == Node.SIMPLE_TYPE)
        {
            bytecode.Add(opcode, opType.typeCode, 0, opName);
        }
        else
        {
            throw new PascalError(node.token, "can't do " + opName +
                                 " operands of type " + opType.Print());
        }
    }

    // Adds the address of the node to the bytecode.
    public void GenerateAddressBytecode(ByteCode bytecode, Node node, SymbolTable symbolTable)
    {
        switch (node.nodeType)
        {
            case Node.IDENTIFIER:
                var symbolLookup = node.symbolLookup;

                int ix;
                if (symbolLookup.symbol.byReference)
                {
                    // By reference, the address is all we need.
                    ix = Inst.defs.LVA;
                }
                else
                {
                    // Load its address.
                    ix = Inst.defs.LDA;
                }
                bytecode.Add(ix, symbolLookup.level,
                             symbolLookup.symbol.address, "address of " + node.Print());
                break;

            case Node.ARRAY:
                var arrayType = node.variable.expressionType;

                // We compute the strides of the nested arrays as we go.
                var strides = new Stack<int>();

                // Start with the array's element size.
                strides.Push((int)arrayType.elementType.GetTypeSize());

                for (int i = 0; i < node.indices.Length; i++)
                {
                    // Generate value of index.
                    this.GenerateBytecode(bytecode, node.indices[i], symbolTable);

                    // Subtract lower bound.
                    var low = arrayType.ranges[i].GetRangeLowBound();
                    var cindexARRAY = bytecode.AddConstant(low);
                    bytecode.Add(Inst.defs.LDC, Inst.defs.I, cindexARRAY, "lower bound " + low);
                    bytecode.Add(Inst.defs.SBI, 0, 0, "subtract lower bound");

                    // Add new stride.
                    var size = (int)arrayType.ranges[i].GetRangeSize();
                    strides.Push(strides.ElementAt(strides.Count - 1) * size);

                    // This would be a good place to do a runtime bounds check since
                    // we have the index and the size. The top of the stack should be
                    // non-negative and less than size.
                }

                // Pop the last stride, we don't need it. It represents the size of the
                // entire array.
                strides.Pop();

                // Look up address of array.
                this.GenerateAddressBytecode(bytecode, node.variable, symbolTable);

                for (var i = 0; i < node.indices.Length; i++)
                {
                    // Compute address of the slice or element.
                    var stride = strides.Pop();
                    bytecode.Add(Inst.defs.IXA, 0, stride,
                                 "address of array " +
                                 ((i == node.indices.Length - 1) ? "element" : "slice") +
                                 " (size " + stride + ")");
                }
                break;

            case Node.FIELD_DESIGNATOR:
                var recordType = node.variable.expressionType;

                // Look up address of record.
                this.GenerateAddressBytecode(bytecode, node.variable, symbolTable);

                // Add the offset of the field.
                var cindex = bytecode.AddConstant(node.field.offset);  //TODO: MVM   (added offset in Node)
                bytecode.Add(Inst.defs.LDC, Inst.defs.I, cindex,
                             "offset of field \"" + node.field.name.print() + "\"");
                bytecode.Add(Inst.defs.ADI, 0, 0, "add offset to record address");
                break;

            case Node.DEREFERENCE:
                // Just push the value of the pointer.
                this.GenerateBytecode(bytecode, node.variable, symbolTable);
                break;

            default:
                throw new PascalError(null, "unknown LHS node " + node.Print());
        }
    }

    // Start a frame for a function/procedure.
    public void BeginExitFrame()
    {
        this.exitInstructions.Push((Array.Empty<int>()).First());  //TODO: MVM
    }

    // Add an address of an instruction to update once we know the end of the function.
    public void AddExitInstruction(int address)
    {
        // this.exitInstructions[this.exitInstructions.Count - 1].push(address); //TODO: MVM
    }

    // End a frame for a function/procedure, returning a list of addresses of UJP functions
    // to update.
    public int[] EndExitFrame() => [this.exitInstructions.Pop()];



}