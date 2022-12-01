using Microsoft.AspNetCore.Http;

namespace AzureFunctionAlert2Slack.Tests
{
    internal class Helpers
    {
        public static async Task<HttpRequest> CreateRequest(HttpMethod method, Uri uri, HttpContent? body = null)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = method.Method;
            httpContext.Request.Scheme = uri.Scheme;
            httpContext.Request.Host = new HostString(uri.Host);
            httpContext.Request.Path = uri.AbsolutePath;
            httpContext.Request.QueryString = new QueryString(uri.Query);
            if (body != null)
            {
                var stream = new MemoryStream();
                await body.CopyToAsync(stream);
                stream.Position = 0;
                httpContext.Request.Body = stream;
            }
            return httpContext.Request;
        }
    }
}
