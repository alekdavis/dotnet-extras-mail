using DotNetExtras.Mail;
using System.Runtime.CompilerServices;

namespace MailLibTests;

public class Data
{
    public string? Zodiac {  get; set; }

    public string? Name { get; set; }

    public int? Year { get; set; }
}

internal static class ProjectSource {
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
    private readonly string _templateFolder;
    private readonly string _templateId;

    public MailTemplateTests()
    {
        _templateFolder = Path.GetFullPath(Path.Combine(ProjectSource.ProjectDirectory(), "Samples", "Zodiac"));
        _templateId = "Zodiac";
    }

    [Theory]
    [InlineData("en-us", "Leo", "Joe", 2025, "en-US")]
    [InlineData("en", "Capricorn", "Mary", 2025, "en-US")]
    [InlineData("en-ca", "Libra", "Jason", 2023, "en-US")]
    [InlineData("es", "Cáncer", "José", 2029, "es")]
    [InlineData("es-mx", "Cáncer", "José", 2027, "es")]
    [InlineData("ru", "Близнецы", "Фёдор", 2026, "ru")]
    [InlineData("ru-ka", "Весы", "Олеся", 2019, "ru")]
    [InlineData("fr", "les Gémeaux", "André", 2030, "en-US")]
    public void Merge
    (
        string language,
        string zodiac,
        string name,
        int year,
        string actualLanguage
    )
    {
        MailTemplate template = new();

        Data data = new()
        {
            Zodiac = zodiac,
            Name = name,
            Year = year
        };
        
        template.Load(_templateFolder, _templateId, language, ".html", data);

        Assert.Equal(actualLanguage, template.Language, true);
        Assert.Contains(data.Zodiac, template.Subject);
        Assert.Contains(data.Year.ToString() ?? "", template.Body);
        Assert.Contains(data.Name ?? "", template.Body);

        // Because tests execute in random order, we can't see if the template is cached,
        // but if we do another merge, it should definitely be cached.
        template.Load(_templateFolder, _templateId, language, ".html", data);
        Assert.True(template.Cached);
    }
}