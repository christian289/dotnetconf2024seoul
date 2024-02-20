namespace DataStructure;

public struct CMethod : IJsonRpcMethod
{
    public CMethod()
    {
        MethodName = GetMethodName(nameof(CMethod));
    }

    public string MethodName { get; set; }

    private string GetMethodName(string typeName)
    {
        return typeName[..1];
    }
}