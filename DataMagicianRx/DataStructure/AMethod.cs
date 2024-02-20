namespace DataStructure;

public struct AMethod : IJsonRpcMethod
{
    public AMethod()
    {
        MethodName = GetMethodName(nameof(AMethod));
    }

    public string MethodName { get; set; }

    private string GetMethodName(string typeName)
    {
        return typeName[..1];
    }
}
