namespace DataStructure;

public struct BMethod : IJsonRpcMethod
{
    public BMethod()
    {
        MethodName = GetMethodName(nameof(BMethod));
    }

    public string MethodName { get; set; }

    private string GetMethodName(string typeName)
    {
        return typeName[..1];
    }
}
