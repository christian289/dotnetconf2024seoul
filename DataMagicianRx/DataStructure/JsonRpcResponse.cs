namespace DataStructure;

public sealed class JsonRpcResponse
{
    public JsonRpcResponse()
    {
        JsonRpc = "2.0";
    }

    [JsonProperty("jsonrpc")]
    public string JsonRpc { get; set; }

    [JsonProperty("result")]
    public object Result { get; set; }

    [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
    public object? Error { get; set; }

    [JsonProperty("request_id")]
    public int RequestId { get; set; }
}
