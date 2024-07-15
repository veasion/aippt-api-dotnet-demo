
using System;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json.Nodes;

class Program
{
    static async Task Main(string[] args)
    {
        // 官网 https://docmee.cn
        // 开放平台 https://docmee.cn/open-platform/api

        // 填写你的API-KEY
        string apiKey = "YOUR API-KEY";

        // 第三方用户ID（数据隔离）
        string uid = "test";
        string subject = "AI未来的发展";

        // 创建 api token (有效期2小时，建议缓存到redis，同一个 uid 创建时之前的 token 会在10秒内失效)
        string? apiToken = await Api.CreateApiToken(apiKey, uid, null);
        Console.WriteLine("apiToken: " + apiToken);
        if (apiToken == null) return;

        // 示例一：流式生成 PPT
        await AipptDemo1(apiToken, subject);

        // 示例二：直接生成 PPT
        // await AipptDemo2(apiToken, subject);

    }

    public static async Task AipptDemo1(string apiToken, string subject)
    {
        // 生成大纲
        Console.WriteLine("\n\n========== 正在生成大纲 ==========");
        string outline = await Api.GenerateOutline(apiToken, subject, null, null);

        // 生成大纲内容
        Console.WriteLine("\n\n========== 正在生成大纲内容 ==========");
        string markdown = await Api.GenerateContent(apiToken, outline, null, null);

        // 随机一个模板
        Console.WriteLine("\n\n========== 随机选择模板 ==========");
        string? templateId = await Api.RandomOneTemplateId(apiToken);
        Console.WriteLine("templateId: " + templateId);

        // 生成PPT
        Console.WriteLine("\n\n========== 正在生成PPT ==========");
        var pptInfo = await Api.GeneratePptx(apiToken, templateId, markdown, false);
        string? pptId = pptInfo?["id"]?.ToString();
        Console.WriteLine("pptId: " + pptId);
        Console.WriteLine("ppt主题：" + pptInfo?["subject"]);
        Console.WriteLine("ppt封面：" + pptInfo?["coverUrl"] + "?token=" + apiToken);

        // 下载PPT到桌面
        Console.WriteLine("\n\n========== 正在下载PPT ==========");
        string? url = await Api.DownloadPptx(apiToken, pptId);
        Console.WriteLine("ppt链接：" + url);
        string savePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + pptId + ".pptx";
        await HttpUtils.DownloadAsync(url, savePath);
        Console.WriteLine("ppt下载完成，保存路径：" + savePath);
    }

    public static async Task AipptDemo2(string apiToken, string subject)
    {
        // 直接生成PPT
        Console.WriteLine("\n\n========== 正在生成PPT ==========");
        var pptInfo = await Api.DirectGeneratePptx(apiToken, true, null, subject);
        string? pptId = pptInfo?["id"]?.ToString();
        string? fileUrl = pptInfo?["fileUrl"]?.ToString();
        Console.WriteLine();
        Console.WriteLine("pptId: " + pptId);
        Console.WriteLine("ppt主题：" + pptInfo?["subject"]);
        Console.WriteLine("ppt封面：" + pptInfo?["coverUrl"] + "?token=" + apiToken);
        Console.WriteLine("ppt链接：" + fileUrl);

        // 下载PPT到桌面
        string savePath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + pptId + ".pptx";
        await HttpUtils.DownloadAsync(fileUrl, savePath);
        Console.WriteLine("ppt下载完成，保存路径：" + savePath);
    }
}