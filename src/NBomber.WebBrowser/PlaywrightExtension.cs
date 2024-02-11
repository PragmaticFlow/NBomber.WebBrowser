using Microsoft.Playwright;
using NBomber.CSharp;

namespace NBomber.WebBrowser.Playwright;

public static class PlaywrightExtension
{
    public static async Task<Contracts.Response<IResponse>> ToNBomberResponse(this IResponse response)
    {
        var respBody = await response.BodyAsync();
        var respHeaders = await response.AllHeadersAsync();
        var reqHeaders = await response.Request.AllHeadersAsync();

        var sizeBytes = respBody.Length + respHeaders.Sum(kv => kv.Value.Length + kv.Key.Length) 
                                        + (response.Request.PostDataBuffer?.Length ?? 0) 
                                        + reqHeaders.Sum(kv => kv.Value.Length + kv.Key.Length);

        return Response.Ok(payload: response, statusCode: response.Status.ToString(), sizeBytes: sizeBytes);
    }
}