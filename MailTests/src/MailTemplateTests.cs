using DotNetExtras.Mail;
using System.Runtime.CompilerServices;
using System.Text;

namespace MailLibTests;

public class Data
{
    public string? Zodiac { get; set; }

    public string? Name { get; set; }

    public int? Year { get; set; }
}

internal static class ProjectSource
{
    private static string CallerFilePath
    (
        [CallerFilePath] string? callerFilePath = null
    )
    {
        return callerFilePath ?? throw new ArgumentNullException(nameof(callerFilePath));
    }

    public static string ProjectDirectory()
    {
        return Path.GetDirectoryName(Path.GetDirectoryName(CallerFilePath())!)!;
    }
}

public class MailTemplateTests
{
    private readonly string _templateFolderOne;
    private readonly string _templateFolderTwo;
    private readonly string _templateId;

    public MailTemplateTests()
    {
        _templateFolderOne = Path.GetFullPath(Path.Combine(ProjectSource.ProjectDirectory(), "Samples", "ZodiacOne"));
        _templateFolderTwo = Path.GetFullPath(Path.Combine(ProjectSource.ProjectDirectory(), "Samples", "ZodiacTwo"));
        _templateId = "Zodiac";
    }

    [Fact]
    public void Load_MergesTemplateContent()
    {
        MailTemplate template = new();
        Data data = new()
        {
            Zodiac = "Géminis",
            Name = "José",
            Year = 2029
        };

        IMailMessage message = template.Load(_templateFolderOne, _templateId, "es-mx", ".html", data);

        Assert.Equal("es", template.Language, true);
        Assert.Equal("es", message.Language, true);
        Assert.Contains(data.Zodiac, template.Subject);
        Assert.Contains(data.Zodiac, message.Subject);
        Assert.Contains(data.Name ?? "", template.Body);
        Assert.Contains(data.Name ?? "", message.Body);
        Assert.Contains(data.Year.ToString() ?? "", template.Body);
        Assert.Contains(data.Year.ToString() ?? "", message.Body);
        Assert.Contains("Zodiaco 1", template.Body);
        Assert.Contains("Zodiaco 1", message.Body);
    }

    [Fact]
    public void Load_CachesTemplateAfterFirstLoad()
    {
        string templateFolder = CreateUniqueTemplateFolder();

        MailTemplate template = new();
        Data data = new()
        {
            Zodiac = "Leo",
            Name = "Joe",
            Year = 2025
        };

        template.Load(templateFolder, _templateId, "en-US", ".html", data);
        Assert.False(template.Cached);

        template.Load(templateFolder, _templateId, "en-US", ".html", data);
        Assert.True(template.Cached);
    }

    private string CreateUniqueTemplateFolder()
    {
        string root = Path.Combine(ProjectSource.ProjectDirectory(), "bin", Guid.NewGuid().ToString("N"), nameof(MailTemplateTests));
        string folder = Path.Combine(root, "CacheBehavior");

        Directory.CreateDirectory(folder);

        string templatePath = Path.Combine(folder, $"{_templateId}_en-us.html");

        File.WriteAllText(templatePath, BuildUniqueCacheTemplate());

        return folder;
    }

    private static string BuildUniqueCacheTemplate()
    {
        StringBuilder builder = new();

        _ = builder.AppendLine("<!DOCTYPE html>");
        _ = builder.AppendLine("<html lang=\"en\">");
        _ = builder.AppendLine("<head>");
        _ = builder.AppendLine("<title>Welcome @Raw(Model.Zodiac)!</title>");
        _ = builder.AppendLine("<meta charset=\"utf-8\">");
        _ = builder.AppendLine("</head>");
        _ = builder.AppendLine("<body>");
        _ = builder.AppendLine("<p>Hello @Raw(Model.Name),</p>");
        _ = builder.AppendLine("<p>Your unique cache sign is: @Raw(Model.Zodiac).</p>");
        _ = builder.AppendLine("<p>&copy; @Model.Year</p>");
        _ = builder.AppendLine("</body>");
        _ = builder.AppendLine("</html>");

        return builder.ToString();
    }

    [Fact]
    public void Load_CacheAccountsForFolderLocation()
    {
        Data data = new()
        {
            Zodiac = "Leo",
            Name = "Joe",
            Year = 2025
        };

        MailTemplate firstTemplate = new();
        MailTemplate secondTemplate = new();

        IMailMessage firstMessage = firstTemplate.Load(_templateFolderOne, _templateId, "en-US", ".html", data);
        IMailMessage secondMessage = secondTemplate.Load(_templateFolderTwo, _templateId, "en-US", ".html", data);

        Assert.Contains("Zodiac 1", firstMessage.Body);
        Assert.Contains("Zodiac 2", secondMessage.Body);
        Assert.DoesNotContain("Zodiac 2", firstMessage.Body);
        Assert.DoesNotContain("Zodiac 1", secondMessage.Body);
    }

    [Theory]
    [InlineData("One", "en-US", "Leo", "en-US", "Welcome Leo 1!")]
    [InlineData("One", "es", "Géminis", "es", "¡Bienvenida Géminis 1!")]
    [InlineData("One", "ru", "Близнецы", "ru", "Встречайте Близнецы 1!")]
    [InlineData("Two", "en-US", "Leo", "en-US", "Welcome Leo 2!")]
    [InlineData("Two", "es", "Géminis", "es", "¡Bienvenida Géminis 2!")]
    [InlineData("Two", "ru", "Близнецы", "ru", "Встречайте Близнецы 2!")]
    public void Load_ReturnsExpectedSubjectForLocalizedTemplates
    (
        string folder,
        string language,
        string zodiac,
        string actualLanguage,
        string expectedSubject
    )
    {
        string templateFolder = folder == "One"
            ? _templateFolderOne
            : _templateFolderTwo;

        MailTemplate template = new();
        Data data = new()
        {
            Zodiac = zodiac,
            Name = "Joe",
            Year = 2025
        };

        IMailMessage message = template.Load(templateFolder, _templateId, language, ".html", data);

        Assert.Equal(actualLanguage, template.Language, true);
        Assert.Equal(actualLanguage, message.Language, true);
        Assert.Equal(expectedSubject, template.Subject);
        Assert.Equal(expectedSubject, message.Subject);
    }
}
