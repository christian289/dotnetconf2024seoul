namespace DataStructure;

public struct JsonRpcRequest(IJsonRpcMethod method, int req_id, params object[] param)
{
    [JsonProperty("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    [JsonProperty("method")]
    public string Method { get; set; } = method.MethodName;

    [JsonProperty("request_id")]
    public int RequestId { get; set; } = req_id;

    [JsonProperty("params", NullValueHandling = NullValueHandling.Ignore)]
    public object[] Params { get; set; } = param;

    public string Serialize() => JsonConvert.SerializeObject(this, Formatting.Indented);
}
