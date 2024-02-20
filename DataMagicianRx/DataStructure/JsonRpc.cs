namespace DataStructure;

public struct JsonRpc(IJsonRpcMethod method, int reqId, params object[] param)
{
    [JsonProperty("jsonrpc")]
    public string Version { get; set; } = "2.0";

    [JsonProperty("method")]
    public string Method { get; set; } = method.MethodName;

    [JsonProperty("request_id")]
    public int RequestId { get; set; } = reqId;

    [JsonProperty("params", NullValueHandling = NullValueHandling.Ignore)]
    public object[] Params { get; set; } = param;

    public string Serialize() => JsonConvert.SerializeObject(this, Formatting.Indented);
}
