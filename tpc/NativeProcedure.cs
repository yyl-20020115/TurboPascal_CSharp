namespace TPC;

// The object that's stored in the Native store.
internal class NativeProcedure(string name, Node returnType, List<Node> parameterTypes, object fn)
{
    public string name = name;
    public Node returnType = returnType;
    public List<Node> parameterTypes = parameterTypes;
    public object fn = fn;
}
