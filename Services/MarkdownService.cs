using Markdig;

namespace MauiApp1.Services;

public class MarkdownService
{
    private readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    public string ToHtml(string markdown)
    {
        return Markdown.ToHtml(markdown ?? "", _pipeline);
    }
}
