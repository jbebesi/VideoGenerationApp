using System.Net;
using System.Text;

namespace VideoGenerationApp.IntegrationTests.Infrastructure
{
    /// <summary>
    /// Mock HttpMessageHandler for testing HTTP requests
    /// </summary>
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new();
        private readonly List<HttpRequestMessage> _requests = new();

        /// <summary>
        /// Gets all captured HTTP requests
        /// </summary>
        public IReadOnlyList<HttpRequestMessage> Requests => _requests.AsReadOnly();

        /// <summary>
        /// Gets the last HTTP request
        /// </summary>
        public HttpRequestMessage? LastRequest => _requests.LastOrDefault();

        /// <summary>
        /// Gets the request body content as string
        /// </summary>
        public async Task<string?> GetRequestBodyAsync(int index = -1)
        {
            if (_requests.Count == 0) return null;

            var request = index < 0 ? _requests.Last() : _requests[index];
            if (request.Content == null) return null;

            return await request.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// Enqueue a response to be returned
        /// </summary>
        public void EnqueueResponse(HttpResponseMessage response)
        {
            _responses.Enqueue(response);
        }

        /// <summary>
        /// Enqueue a JSON response with OK status
        /// </summary>
        public void EnqueueJsonResponse(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            _responses.Enqueue(response);
        }

        /// <summary>
        /// Clear all captured requests and responses
        /// </summary>
        public void Clear()
        {
            _requests.Clear();
            _responses.Clear();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Clone the request to preserve the content for inspection
            var clonedRequest = await CloneHttpRequestMessageAsync(request);
            _requests.Add(clonedRequest);

            if (_responses.Count == 0)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                };
            }

            return _responses.Dequeue();
        }

        private async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage req)
        {
            var clone = new HttpRequestMessage(req.Method, req.RequestUri)
            {
                Version = req.Version
            };

            if (req.Content != null)
            {
                var originalContent = await req.Content.ReadAsStringAsync();
                clone.Content = new StringContent(originalContent, Encoding.UTF8, req.Content.Headers.ContentType?.MediaType ?? "application/json");
            }

            foreach (var header in req.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            foreach (var property in req.Options)
            {
                clone.Options.TryAdd(property.Key, property.Value);
            }

            return clone;
        }
    }
}
