using PuppeteerSharp;

namespace NBomber.WebBrowser.PuppeteerSharp;

public static class PuppeteerSharpExtension
{
    public static async Task<Contracts.Response<IResponse>> ToNBomberResponse(this IResponse response)
    {
        var respBody =  await response.BufferAsync();
        var respHeaders = response.Headers;
        var reqHeaders = response.Request.Headers;

        var sizeBytes = respBody.Length + respHeaders.Sum(kv => kv.Value.Length + kv.Key.Length)
                                        + (response.Request.PostData is byte[] bytes ? bytes.Length : 0) 
                                        + reqHeaders.Sum(kv => kv.Value.Length + kv.Key.Length);

        return CSharp.Response.Ok(payload: response, statusCode: response.Status.ToString(), sizeBytes: sizeBytes);
    }
}