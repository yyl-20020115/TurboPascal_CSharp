namespace TPC;

// Tracks a list of native functions that can be called from Pascal.
internal class Native
{
    private readonly List<NativeProcedure> nativeProcedures = [];

    public Native()
    {
        // List of NativeProcedure objects. The index within the array is the
        // number passed to the "CSP" instruction.
    }

    // Adds a native method, returning its index.
    public int Add(NativeProcedure nativeProcedure)
    {
        var index = this.nativeProcedures.Count;
        this.nativeProcedures.Add(nativeProcedure);
        return index;
    }

    // Get a native method by index.
    public NativeProcedure Get(int index) => this.nativeProcedures[index];
}
