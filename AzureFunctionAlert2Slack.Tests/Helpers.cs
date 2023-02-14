using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

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

        public static async Task<HttpRequest> CreatePostJsonRequest(object body, Uri? uri = null)
        {
            return await CreateRequest(HttpMethod.Post,
            uri ?? new Uri("https://localhost"),
            new StringContent(
                    JsonConvert.SerializeObject(body, new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() }),
                    new System.Net.Http.Headers.MediaTypeHeaderValue("application/json")));
        }
    }
}
