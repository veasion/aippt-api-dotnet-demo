using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class Api
{

    public const string BASE_URL = "https://docmee.cn/api";

    public static async Task<string?> CreateApiToken(string apiKey, string userId)
    {
        string url = BASE_URL + "/user/createApiToken";
        var body = JsonConvert.SerializeObject(new
        {
            uid = userId
        });
        var headers = new Dictionary<string, string>() {{
            "Api-Key", apiKey
        }};
        var resp = await HttpUtils.PostJsonAsync(url, headers, body);
        if (resp.StatusCode != 200 || resp.Text == null)
        {
            throw new Exception("创建apiToken失败，httpStatus=" + resp.StatusCode);
        }
        var json = JObject.Parse(resp.Text);
        if (json.TryGetValue("code", out var code) && (int)code != 0)
        {
            throw new Exception("创建apiToken异常：" + json["message"]);
        }
        return json["data"]?["token"]?.ToString();
    }

    public static async Task<string> GenerateOutline(string apiToken, string subject, string? prompt = null, string? dataUrl = null)
    {
        string url = BASE_URL + "/ppt/generateOutline";
        var body = JsonConvert.SerializeObject(new
        {
            subject,
            prompt,
            dataUrl
        });
        var headers = new Dictionary<string, string>() {{
            "token", apiToken
        } };
        List<string> sb = new List<string>();
        var resp = await HttpUtils.PostSseAsync(url, headers, body, (json) => HandleStreamData(json, sb));
        if (resp.StatusCode != 200)
        {
            throw new Exception("生成大纲失败，httpStatus=" + resp.StatusCode);
        }
        if (resp.ContentType.Contains("application/json") && resp.Text != null)
        {
            var json = JObject.Parse(resp.Text);
            throw new Exception("生成大纲异常：" + json["message"]);
        }
        return string.Join("", sb);
    }

    public static async Task<string> GenerateContent(string apiToken, string outlineMarkdown, string? prompt = null, string? dataUrl = null)
    {
        string url = BASE_URL + "/ppt/generateContent";
        var body = JsonConvert.SerializeObject(new
        {
            outlineMarkdown,
            prompt,
            dataUrl
        });
        var headers = new Dictionary<string, string>() {{
            "token", apiToken
        } };
        List<string> sb = new List<string>();
        var resp = await HttpUtils.PostSseAsync(url, headers, body, (json) => HandleStreamData(json, sb));
        if (resp.StatusCode != 200)
        {
            throw new Exception("生成大纲内容失败，httpStatus=" + resp.StatusCode);
        }
        if (resp.ContentType.Contains("application/json") && resp.Text != null)
        {
            var json = JObject.Parse(resp.Text);
            throw new Exception("生成大纲内容异常：" + json["message"]);
        }
        return string.Join("", sb);
    }

    private static void HandleStreamData(string str, List<string> sb)
    {
        var json = JObject.Parse(str);
        if (json.TryGetValue("status", out var status) && (int)status == -1)
        {
            throw new Exception("请求异常：" + json["error"]);
        }
        if (json.TryGetValue("text", out var text))
        {
            sb.Add(text.ToString());
            Console.Write(text.ToString());
        }
    }

    public static async Task<string?> RandomOneTemplateId(string apiToken)
    {
        string url = BASE_URL + "/ppt/randomTemplates";
        var body = JsonConvert.SerializeObject(new
        {
            size = 1,
            filters = new
            {
                type = 1
            }
        });
        var headers = new Dictionary<string, string>() {{
            "token", apiToken
        }};
        var resp = await HttpUtils.PostJsonAsync(url, headers, body);
        if (resp.StatusCode != 200 || resp.Text == null)
        {
            throw new Exception("获取模板失败，httpStatus=" + resp.StatusCode);
        }
        var json = JObject.Parse(resp.Text);
        if (json.TryGetValue("code", out var code) && (int)code != 0)
        {
            throw new Exception("获取模板异常：" + json["message"]);
        }
        return json["data"]?[0]?["id"]?.ToString();
    }

    public static async Task<JToken?> GeneratePptx(string apiToken, string? templateId, string markdown, bool pptxProperty = false)
    {
        string url = BASE_URL + "/ppt/generatePptx";
        var body = JsonConvert.SerializeObject(new
        {
            templateId,
            outlineContentMarkdown = markdown,
            pptxProperty
        });
        var headers = new Dictionary<string, string>() {{
            "token", apiToken
        }};
        var resp = await HttpUtils.PostJsonAsync(url, headers, body);
        if (resp.StatusCode != 200 || resp.Text == null)
        {
            throw new Exception("生成PPT失败，httpStatus=" + resp.StatusCode);
        }
        var json = JObject.Parse(resp.Text);
        if (json.TryGetValue("code", out var code) && (int)code != 0)
        {
            throw new Exception("生成PPT异常：" + json["message"]);
        }
        return json["data"]?["pptInfo"];
    }

    public static async Task<string?> DownloadPptx(string apiToken, string id)
    {
        string url = BASE_URL + "/ppt/downloadPptx";
        var body = JsonConvert.SerializeObject(new
        {
            id
        });
        var headers = new Dictionary<string, string>() {{
            "token", apiToken
        }};
        var resp = await HttpUtils.PostJsonAsync(url, headers, body);
        if (resp.StatusCode != 200 || resp.Text == null)
        {
            throw new Exception("下载PPT失败，httpStatus=" + resp.StatusCode);
        }
        var json = JObject.Parse(resp.Text);
        if (json.TryGetValue("code", out var code) && (int)code != 0)
        {
            throw new Exception("下载PPT异常：" + json["message"]);
        }
        return json["data"]?["fileUrl"]?.ToString();
    }

    public static async Task<JToken?> DirectGeneratePptx(string apiToken, bool stream, string? templateId, string subject, string? prompt = null, string? dataUrl = null, bool pptxProperty = false)
    {
        string url = BASE_URL + "/ppt/directGeneratePptx";
        var body = JsonConvert.SerializeObject(new
        {
            stream,
            templateId,
            subject,
            prompt,
            dataUrl,
            pptxProperty
        });
        var headers = new Dictionary<string, string>() {{
            "token", apiToken
        } };
        if (stream)
        {
            List<JToken?> pptInfo = new List<JToken?>();
            var resp = await HttpUtils.PostSseAsync(url, headers, body, (json) => HandleStreamPptxData(json, pptInfo));
            if (resp.StatusCode != 200)
            {
                throw new Exception("生成PPT失败，httpStatus=" + resp.StatusCode);
            }
            if (resp.ContentType.Contains("application/json") && resp.Text != null)
            {
                var json = JObject.Parse(resp.Text);
                throw new Exception("生成PPT异常：" + json["message"]);
            }
            return pptInfo[0];
        }
        else
        {
            var resp = await HttpUtils.PostJsonAsync(url, headers, body);
            if (resp.StatusCode != 200 || resp.Text == null)
            {
                throw new Exception("生成PPT失败，httpStatus=" + resp.StatusCode);
            }
            var json = JObject.Parse(resp.Text);
            if (json.TryGetValue("code", out var code) && (int)code != 0)
            {
                throw new Exception("生成PPT异常：" + json["message"]);
            }
            return json["data"]?["pptInfo"];
        }
    }

    private static void HandleStreamPptxData(string str, List<JToken?> pptInfo)
    {
        var json = JObject.Parse(str);
        json.TryGetValue("status", out var status);
        if (status != null && (int)status == -1)
        {
            throw new Exception("请求异常：" + json["error"]);
        }
        if (status != null && (int)status == 4 && json.TryGetValue("result", out var result))
        {
            pptInfo.Add(result);
        }
        if (json.TryGetValue("text", out var text))
        {
            Console.Write(text.ToString(), Console.BufferWidth);
        }
    }

}