namespace DataStructure;

public struct AMethod : IJsonRpcMethod
{
    public AMethod()
    {
        MethodName = GetMethodName(nameof(AMethod));
    }

    public string MethodName { get; set; }

    private string GetMethodName(string type_name)
    {
        return type_name[..1];
    }
}
