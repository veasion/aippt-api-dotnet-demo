using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class HttpUtils
{

    static HttpClient client = new HttpClient();

    public static async Task<HttpResponse> PostSseAsync(string url, Dictionary<string, string> headers, string body, Action<string> consumer)
    {
        using (var request = new HttpRequestMessage(HttpMethod.Post, url))
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }
            using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
            {
                var contentType = response.Content.Headers.GetValues("Content-Type").FirstOrDefault("");
                if (response.IsSuccessStatusCode && contentType.Contains("text/event-stream"))
                {
                    var stream = await response.Content.ReadAsStreamAsync();
                    using (var reader = new StreamReader(stream))
                    {
                        while (!reader.EndOfStream)
                        {
                            var line = await reader.ReadLineAsync();
                            if (line != null && line.StartsWith("data:"))
                            {
                                var data = line.Substring(line.StartsWith("data: ") ? 6 : 5);
                                if (string.IsNullOrEmpty(data) || data == "[DONE]")
                                {
                                    continue;
                                }
                                consumer(data);
                            }
                        }
                    }
                    return new HttpResponse((int)response.StatusCode, contentType, null);
                }
                else
                {
                    var text = await response.Content.ReadAsStringAsync();
                    return new HttpResponse((int)response.StatusCode, contentType, text);
                }
            }
        }
    }

    public static async Task<HttpResponse> PostJsonAsync(string url, Dictionary<string, string> headers, string body)
    {
        using (var request = new HttpRequestMessage(HttpMethod.Post, url))
        {
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }
            using (var response = await client.SendAsync(request))
            {
                var text = await response.Content.ReadAsStringAsync();
                var contentType = response.Content.Headers.GetValues("Content-Type").FirstOrDefault("");
                return new HttpResponse((int)response.StatusCode, contentType, text);
            }
        }
    }

    public static async Task DownloadAsync(string url, string savePath)
    {
        using (var response = await client.GetAsync(url))
        {
            if (response.IsSuccessStatusCode)
            {
                var fileBytes = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(savePath, fileBytes);
            }
        }
    }

    public class HttpResponse
    {
        int statusCode;
        string contentType;
        string? text;

        public HttpResponse(int statusCode, string contentType, string? text)
        {
            this.statusCode = statusCode;
            this.contentType = contentType;
            this.text = text;
        }
        public int StatusCode { get { return statusCode; } }
        public string ContentType { get { return contentType; } }
        public string? Text { get { return text; } }
    }

}