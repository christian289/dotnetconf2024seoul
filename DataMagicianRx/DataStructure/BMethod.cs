namespace DataStructure;

public struct BMethod : IJsonRpcMethod
{
    public BMethod()
    {
        MethodName = GetMethodName(nameof(BMethod));
    }

    public string MethodName { get; set; }

    private string GetMethodName(string type_name)
    {
        return type_name[3..6];
    }
}
