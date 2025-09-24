namespace TPC;

// The bytecode object. Stores all bytecodes for a program, along with accompanying
// data (such as program constants).
internal class ByteCode(Native native)
{
    public List<double> istore = [];
    public List<object> constants = [];
    public List<object> typedConstants = [];
    public int startAddress = 0;
    private readonly Dictionary<int,string> comments = [];
    public Native native = native;

    // Add a constant (of any type), returning the cindex.
    public int AddConstant(object c)
    {
        // Re-use existing constants. We could use a hash table for this.
        for (var i = 0; i < this.constants.Count; i++)
        {
            if (c == this.constants[i])
            {
                return i;
            }
        }

        // Add new constants.
        this.constants.Add(c);
        return this.constants.Count - 1;
    }

    // Add an array of words to the end of the typed constants. Returns the
    // address of the item that was just added.
    public int AddTypedConstants(object raw)
    {
        var address = this.typedConstants.Count;

        // Append entire "raw" array to the back of the typedConstants array.
        //TODO: MVM this.typedConstants.push.apply(this.typedConstants, raw);  //TODO: MVM

        return address;
    }

    // Add an opcode to the istore.
    public void Add(int opcode,int operand1,int operand2, string comment)
    {
        var i = Inst.defs.Make(opcode, operand1, operand2);
        var address = this.GetNextAddress();
        this.istore.Add(i);
        if (comment != null)
        {
            this.AddComment(address, comment);
        }
    }

    // Replace operand2 of the instruction.
    public void SetOperand2(int address,int operand2)
    {
        var i = this.istore[address];
        i = Inst.defs.Make(Inst.defs.GetOpcode((int)i), Inst.defs.GetOperand1((int)i), operand2);
        this.istore[address] = i;
    }

    // Return the next address to be added to the istore.
    public int GetNextAddress() => this.istore.Count;

    // Return a printable version of the bytecode object.
    public string Print() => $"{this.PrintConstants()}\n{this.PrintIstore()}";

    // Set the starting address to the next instruction that will be added.
    public void SetStartAddress() => this.startAddress = this.GetNextAddress();

    // Add a comment to the address.
    public void AddComment(int address,string comment)
    {
        var existingComment = this.comments[address];
        if (existingComment != null)
        {
            // Add to existing comment.
            comment = existingComment + "; " + comment;
        }
        this.comments[address] = comment;
    }

    // Return a printable version of the constant table.
    public string PrintConstants()
    {
        List<string> lines = [];
        for (int i = 0; i < this.constants.Count; i++)
        {
            var value = this.constants[i];
            if (value.GetType() == typeof(string))
            {
                value = $"'{value}'";
            }
            lines.Add(Utils.RightAlign(i.ToString(), 4) + ": " + value);
        }

        return $"Constants:\n{string.Join("\n", lines.ToArray())}\n";
    }

    // Return a printable version of the istore array.
    public string PrintIstore()
    {
        List<string> lines = [];
        for (int address = 0; address < this.istore.Count; address++)
        {
            var line = Utils.RightAlign(address.ToString(), 4) + ": " +
                Utils.LeftAlign(Inst.defs.Disassemble((int)this.istore[address]), 11);
            var comment = this.comments[address];
            if (comment != null)
            {
                line += $" ; {comment}";
            }
            lines.Add(line);
        }

        return $"Istore:\n{string.Join("\n", lines.ToArray())}\n";
    } 
}
