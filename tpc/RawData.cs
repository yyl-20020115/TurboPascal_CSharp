namespace TPC;

// An object that stores a linear array of raw data (constants) and a parallel
// array of their simple type codes.
internal class RawData
{
    internal int length;
    internal List<object> data;
    internal List<int> simpleTypeCodes;

    public RawData()
    {
        this.length = 0;
        this.data = [];
        this.simpleTypeCodes = [];
    }

    // Adds a piece of data and its simple type (inst.I, etc.) to the list.
    public void Add(object datum, int simpleTypeCode)
    {
        this.length++;
        this.data.Add(datum);
        this.simpleTypeCodes.Add(simpleTypeCode);
    }

    // Adds a SIMPLE_TYPE node.
    public void AddNode(Node node)
    {
        this.Add(node.GetConstantValue(), node.expressionType.GetSimpleTypeCode());
    }

    // Print the array for human debugging.
    public string Print() => "(" + string.Join(", ", data.ToArray()) + ")";
}
