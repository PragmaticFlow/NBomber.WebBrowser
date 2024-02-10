using Microsoft.Playwright;
using NBomber.CSharp;

namespace NBomber.WebBrowser;

public static class PlaywrightExtension
{
    public static Contracts.Response<IResponse> ToNBomberResponse(this IResponse response)
    {
        var respBody =  response.BodyAsync().Result.Length;
        var respHeaders = response.AllHeadersAsync().Result.Sum(kv => kv.Value.Length + kv.Key.Length);

        var reqBody = response.Request.PostDataBuffer?.Length;
        var reqHeaders = response.Request.AllHeadersAsync().Result.Sum(kv => kv.Value.Length + kv.Key.Length);

        var sizeBytes = respBody + respHeaders + (reqBody ?? 0) + reqHeaders;

        /*return Response.Ok(payload: response, statusCode: response.Status.ToString(), sizeBytes: sizeBytes);*/

        return response.Ok ? 
            Response.Ok(payload: response, sizeBytes: sizeBytes) : 
            Response.Fail(payload: response, sizeBytes: sizeBytes);
    }
}