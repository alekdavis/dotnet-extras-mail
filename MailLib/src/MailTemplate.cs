using DotNetExtras.Common.Json;
using HtmlAgilityPack;
using RazorLight;
using RazorLight.Caching;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace DotNetExtras.Mail;

/// <summary>
/// Generates an email message for the specific (or matching) language from a localized 
/// <see href="https://learn.microsoft.com/en-us/aspnet/core/mvc/views/razor">Razor template</see>
/// file and the provided data object.
/// </summary>
/// <remarks>
/// <para>
/// The merge process makes these assumptions and conforms to the following conventions:
/// <list type="bullet">
/// <item>Every email template is identified by a string ID, such as <c>EmailVerification</c>, <c>WelcomeMessage</c>, etc.</item>
/// <item>Localized template files are named as <c>templateId_languageCode.extension</c>, such as <c>Welcome_es-mx.html</c> (separator characters can be customized).</item>
/// <item>Localized template files are formatted as valid HTML documents.</item>
/// <item>The contents of the <c>&lt;title&gt;</c> elements in the localized template files will be used as the email message subjects.</item>
/// <item>The contents of the <c>&lt;body&gt;</c> elements in the localized template files will be used as the email message bodies.</item>
/// <item>Template files can contain Razor syntax for data binding.</item>
/// <item>The specified language code must match the language code suffix in the file name.</item>
/// <item>If no template file with the language code suffix matching the specified language code is found for a particular email template ID, then an alternative language code will be used.</item>
/// <item>If a more specific language code (e.g. <c>es-mx</c>) is not implemented for the specified template, a template file with a more generic language code (e.g. <c>es</c>) will be tried.</item>
/// <item>A language map can be defined for more precise language code mapping.</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// Zodiac_en-us.html file:
/// <code language="html">
/// &lt;!DOCTYPE html&gt;
/// &lt;html lang="en"&gt;
/// &lt;head&gt;
/// &lt;title&gt;Welcome @Raw(Model.Zodiac)!&lt;/title&gt;
/// &lt;meta charset="utf-8"&gt;
/// &lt;/head&gt;
/// &lt;body&gt;
/// &lt;p&gt;Hello @Raw(Model.Name),&lt;/p&gt;
/// &lt;p&gt;
/// Your Zodiac sign is: @Raw(Model.Zodiac).
/// &lt;/p&gt;
/// &lt;p&gt;
/// &amp;copy; @Model.Year | &lt;a href="#"&gt;Terms&lt;/a&gt; | &lt;a href="#"&gt;Privacy&lt;/a&gt; | &lt;a href="#"&gt;Unsubscribe&lt;/a&gt;
/// &lt;/p&gt;
/// &lt;/body&gt;
/// &lt;/html&gt;
/// </code>
/// 
/// C# code:
/// <code>
/// MailTemplate template = new();
///
/// Data data = new()
/// {
///     Zodiac = "Leo",
///     Name = "John",
///     Year = 2025
/// };
/// 
/// // Load the en-US version of the Zodiac template from the Samples/Zodiac folder and merge it with data.
/// template.Load("Samples/Zodiac", "Zodiac", "en-US", ".html", data);
/// 
/// // Subject will hold the merged value of the title element.
/// string subject = template.Subject;
/// 
/// // Body will hold the merged value of the body element.
/// string body = template.Body;
/// 
/// // Language will hold the language code actually used by the template.
/// string language = template.Language;
/// </code>
/// </example>
public partial class MailTemplate
{
    #region Private properties
    // Default language if localized version is not supported.
    private readonly string? _defaultLanguage = null;

    // Default template file extension. 
    private readonly string? _defaultTemplateFileExtension = null;

    // Separates template ID from language code in template file name, such as "NewAccountActivation-en".
    private readonly string? _languageSeparator = null;

    // Separates language code parts, such as in "en_US".
    private readonly string? _subLanguageSeparator = null;

    // Map of non-standard language fallbacks.
    private readonly Dictionary<string, string>? _languageMap = null;

    // Localization language.
    private string? _language = null;

    // Path to the template file.
    private string? _path = null;

    // Cache key for the localized template.
    private string? _key = null;

    // Cache localized template keys.
    private static readonly ConcurrentDictionary<string, string> _cachedKeys = new();

    // Cache localized template file paths.
    private static readonly ConcurrentDictionary<string, string> _cachedPaths = new();

    // Cache languages.
    private static readonly ConcurrentDictionary<string, string> _cachedLanguages = new();

    // Cache localized template text values.
    private static readonly ConcurrentDictionary<string, string> _cachedTemplates = new();

    // Used to multi-threaded synchronization and Razor engine initialization.
    private static readonly object _razorLock = new();

    // Need semaphore for Razor operation because lock cannot be used in async call.
    private static readonly SemaphoreSlim _razorSemaphore = new(1, 1);

    // Handles merges (needs to be static so it can use in-memory caching).
    private static RazorLightEngine? _razorEngine = null;
    #endregion

    #region Public properties
    /// <summary>
    /// Returns the original text of the localized HTML email template.
    /// </summary>
    public virtual string? Template { get; private set; } = null;

    /// <summary>
    /// Returns the text of the email HTML message body 
    /// (after the data transformation performed by the <see cref="Load(string, string, string, string?, object?)"/> call).
    /// </summary>
    /// <remarks>
    /// This will be the message body sent to the email recipient.
    /// </remarks>
    public virtual string? Body { get; private set; } = null;

    /// <summary>
    /// Returns the email HTML message subject from the title element 
    /// (after the data transformation performed by the <see cref="Load(string, string, string, string?, object?)"/> call).
    /// </summary>
    /// <remarks>
    /// This will be the message subject sent to the email recipient.
    /// </remarks>
    public virtual string? Subject { get; private set; } = null;

    /// <summary>
    /// Returns the real template language used for the specified 
    /// template ID and language in a pretty format, such as <c>xx-YY-ZZ</c>
    /// (after the data transformation performed by the <see cref="Load(string, string, string, string?, object?)"/> call).
    /// </summary>
    /// <remarks>
    /// This property can be used to determine which language was actually used.
    /// </remarks>
    public virtual string? Language
    {
        get
        {
            if (!string.IsNullOrEmpty(_language))
            {
                string language = _language.Replace('_', '-');

                int index = language.IndexOf('-');

                if (index > 0 && index < language.Length)
                {
                    string left = language[..index];
                    string right = language[index..];

                    language = left.ToLower() + right.ToUpper();
                }

                return language.TrimStart('-').TrimEnd('-');
            }

            return _language;
        }
    }

    /// <summary>
    /// Indicates whether the pre-compiled template was loaded from the Razor engine's memory cache
    /// (after the data transformation performed by the <see cref="Load(string, string, string, string?, object?)"/> call).
    /// </summary>
    public virtual bool Cached
    {
        get; private set;
    }
    #endregion

    #region Constructors    
    /// <summary>
    /// Initializes a new instance of the <see cref="MailTemplate"/> class.
    /// </summary>
    /// <param name="defaultLanguage">
    /// Default language code.
    /// </param>
    /// <param name="defaultTemplateFileExtension">
    /// Default template file extension.
    /// </param>
    /// <param name="languageMap">
    /// Non-standard mapping of language code fallbacks.
    /// </param>
    /// <param name="languageSeparator">
    /// Separates template ID from language code in template file name, such as <c>NewAccountActivation-en</c>.
    /// </param>
    /// <param name="subLanguageSeparator">
    /// Separates language code parts, such as in <c>en_US</c>.
    /// </param>
    public MailTemplate
    (
        string? defaultLanguage = "en-US",
        string? defaultTemplateFileExtension = ".html",
        string? languageSeparator = "_",
        string? subLanguageSeparator = "-",
        Dictionary<string, string>? languageMap = null
    )
    {
        _defaultLanguage = defaultLanguage;
        _defaultTemplateFileExtension = defaultTemplateFileExtension;
        _languageMap = languageMap;
        _languageSeparator = languageSeparator;
        _subLanguageSeparator = subLanguageSeparator;

        Cached = false;

        InitializeRazor();
    }
    #endregion

    #region Public methods
    /// <summary>
    /// Loads a localized email template file for the specified (or matching) language 
    /// and merges the template text with the specified data (if the data object is specified).
    /// </summary>
    /// <param name="folder">
    /// Path to the folder holding the template files (can be relative or absolute).
    /// </param>
    /// <param name="template">
    /// Template identifier that will be used as the beginning of the localized template file name.
    /// </param>
    /// <param name="language">
    /// Preferred template file language code.
    /// </param>
    /// <param name="extension">
    /// Extension of the template file. 
    /// If not specified, the default value set by the <see cref="MailTemplate(string?, string?, string?, string?, Dictionary{string, string}?)">constructor</see> will be used. 
    /// </param>
    /// <param name="data">
    /// Notification data that will be merged with the template file text to generate the message.
    /// </param>
    /// <returns>
    /// A localized mail message template merged with the data in the specified or a fallback language.
    /// </returns>
    /// <remarks>
    /// After calling this method, use the 
    /// <see cref="Subject"/> and <see cref="Body"/>
    /// properties to get the email message subject and body.
    /// The <see cref="Language"/> property will hold the language code of the loaded template
    /// (which may be different from the requested language if the template in the requested language is not available).
    /// </remarks>
    /// <example>
    /// Zodiac_en-us.html file:
    /// <code language="html">
    /// &lt;!DOCTYPE html&gt;
    /// &lt;html lang="en"&gt;
    /// &lt;head&gt;
    /// &lt;title&gt;Welcome @Raw(Model.Zodiac)!&lt;/title&gt;
    /// &lt;meta charset="utf-8"&gt;
    /// &lt;/head&gt;
    /// &lt;body&gt;
    /// &lt;p&gt;Hello @Raw(Model.Name),&lt;/p&gt;
    /// &lt;p&gt;
    /// Your Zodiac sign is: @Raw(Model.Zodiac).
    /// &lt;/p&gt;
    /// &lt;p&gt;
    /// &amp;copy; @Model.Year | &lt;a href="#"&gt;Terms&lt;/a&gt; | &lt;a href="#"&gt;Privacy&lt;/a&gt; | &lt;a href="#"&gt;Unsubscribe&lt;/a&gt;
    /// &lt;/p&gt;
    /// &lt;/body&gt;
    /// &lt;/html&gt;
    /// </code>
    /// 
    /// C# code:
    /// <code>
    /// MailTemplate template = new();
    ///
    /// Data data = new()
    /// {
    ///     Zodiac = "Leo",
    ///     Name = "John",
    ///     Year = 2025
    /// };
    /// 
    /// // Load the en-US version of the Zodiac template from the Samples/Zodiac folder and merge it with data.
    /// template.Load("Samples/Zodiac", "Zodiac", "en-US", ".html", data);
    /// 
    /// // Subject will hold the merged value of the title element.
    /// string subject = template.Subject;
    /// 
    /// // Body will hold the merged value of the body element.
    /// string body = template.Body;
    /// 
    /// // Language will hold the language code actually used by the template.
    /// string language = template.Language;
    /// </code>
    /// </example>
    public virtual void Load
    (
        string folder,
        string template,
        string language,
        string? extension = null,
        object? data = null
    )
    {
        // If we have the language map,
        // translate the preferred language code to the mapped value.
        if (!string.IsNullOrEmpty(language) && _languageMap != null && _languageMap.ContainsKey(language))
        {
            language = _languageMap[language];
        }

        List<string> languages = GetCompatibleLanguages(NormalizeLanguage(language));

        // Cache key for the preferred language.
        string? originalKey = FormatKey(template, language);

        string key;

        foreach (string superLanguage in languages)
        {
            // Generate cache key for the template and language.
            key = FormatKey(template, superLanguage);

            // See if this language key is already mapped in cache.
            if (_cachedKeys.ContainsKey(key))
            {
                key = _cachedKeys[key];
            }

            // Try getting template file path from the cache.
            if (_cachedPaths.ContainsKey(key))
            {
                _key = key;
                _language = _cachedLanguages[key];
                _path = _cachedPaths[key];

                // If the preferred language is not mapped in cache,
                // map it to the found language.
                if (!_cachedKeys.ContainsKey(originalKey))
                {
                    _cachedKeys[originalKey] = key;
                }

                break;
            }
            // Template file path is not in the cache...
            else
            {
                string? fileExtension = extension ?? _defaultTemplateFileExtension;

                fileExtension ??= "";

                // Get file path for this language.
                string path = FormatPath(
                    folder,
                    template,
                    superLanguage,
                    fileExtension);

                if (File.Exists(path))
                {
                    _key = key;
                    _language = superLanguage;
                    _path = path;

                    _cachedPaths[key] = _path;
                    _cachedLanguages[key] = _language;

                    // If the preferred language is not mapped in cache,
                    // map it to the found language.
                    if (!_cachedKeys.ContainsKey(originalKey))
                    {
                        _cachedKeys[originalKey] = key;
                    }

                    break;
                }
            }
        }

        if (_path == null)
        {
            throw new Exception(
                $"Cannot find an HTML mail template '{template}' for the '{language}' language code.");
        }

        if (_key == null)
        {
            throw new Exception(
                $"Cannot determine the key to identify the mail template '{template}' for the '{language}' language code.");
        }

        // Try getting template from cache.
        if (_cachedTemplates.ContainsKey(_key))
        {
            Template = _cachedTemplates[_key];
        }
        // Template has not been cached, yet...
        else
        {
            string text;

            try
            {
                text = File.ReadAllText(_path);
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot read text from the mail template file '{_path}'.", ex);
            }

            // Get template from file and handle non-Razor @ characters.
            Template = NormalizeTemplate(text);

            _cachedTemplates[_key] = Template;
        }

        try
        {
            // If we have notification data, merge them with the template.
            Body = data == null 
                ? Template 
                : Merge(_key, Template, data);
        }
        catch (Exception ex)
        {
            throw new Exception($"Cannot merge mail template with the supplied data '{data.ToJson()}'.", ex);
        }

        // Retrieve email notification subject from the title tag.
        if (!string.IsNullOrEmpty(Body))
        {
            HtmlDocument htmlDoc = new();

            try
            {
                // See: https://html-agility-pack.net/from-string
                htmlDoc.LoadHtml(Body);
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot load HTML content from the mail template in '{_path}'.", ex);
            }

            HtmlNode? htmlNode = null;

            try
            {
                // Get the HTML title node.
                htmlNode = htmlDoc.DocumentNode.SelectSingleNode("//title");
            }
            catch
            {
                // Not a catastrophic error (maybe the title was not intended).
            }

            if (htmlNode != null)
            {
                string title = htmlNode.InnerText;

                if (title != null)
                {
                    Subject = RegexRepeatedSpaceChars().Replace(title, " ").Trim();
                }
            }
        }
    }
    #endregion

    #region Private methods
    /// <summary>
    /// Merges the template with data.
    /// </summary>
    /// <param name="key">
    /// Key identifying localized template.
    /// </param>
    /// <param name="template">
    /// Template text.
    /// </param>
    /// <param name="data">
    /// Notification data.
    /// </param>
    /// <returns>
    /// Notification message body.
    /// </returns>
    private string Merge
    (
        string key,
        string template,
        object data
    )
    {
        Task<string> task = MergeAsync(
            key,
            template,
            data);

        try
        {
            task.Wait();
        }
        catch
        {
            throw;
        }

        return task.Result;
    }

    /// <inheritdoc cref="MergeAsync(string, string, object)" path="param|returns"/>
    /// <summary>
    /// Asynchronous method merging template with data.
    /// </summary>
    private async Task<string> MergeAsync
    (
        string key,
        string template,
        object data
    )
    {
        InitializeRazor();

        TemplateCacheLookupResult? cacheResult = null;

        lock (_razorLock)
        {
#pragma warning disable CS8602
            cacheResult = _razorEngine.Handler.Cache.RetrieveTemplate(key);
#pragma warning restore CS8602
        }

        string result;

        await _razorSemaphore.WaitAsync();
        try
        {
            if (cacheResult != null && cacheResult.Success)
            {
                Cached = true;
                ITemplatePage templatePage = cacheResult.Template.TemplatePageFactory();

                result = await _razorEngine.RenderTemplateAsync(templatePage, data);
            }
            else
            {
                Cached = false;
                result = await _razorEngine.CompileRenderStringAsync(key, template, data);
            }
        }
        finally
        {
            _ = _razorSemaphore.Release();
        }

        return result;
    }

    /// <summary>
    /// Implements special handling of certain template elements.
    /// </summary>
    /// <param name="template">
    /// Template text.
    /// </param>
    /// <returns>
    /// Normalized template.
    /// </returns>
    private static string NormalizeTemplate
    (
        string template
    )
    {
        // If @media element is already escaped, nothing else to do.
        if (template.Contains("@@media", StringComparison.InvariantCultureIgnoreCase))
        {
            return template;
        }

        // Escape @media element.
        return template.Replace("@media", "@@media", StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Takes a language code and returns a list of all relevant languages 
    /// including the original, the fallbacks (if any) and the default.
    /// </summary>
    /// <param name="language">
    /// The preferred language code.
    /// </param>
    /// <returns>
    /// List of possible alternatives.
    /// </returns>
    /// <example>
    /// <c>en-AU</c>: <c>en-AU</c>, <c>en</c>, <c>en-US</c>.
    /// <c>en-EN</c>: <c>en-EN</c>, <c>en</c>, <c>en-US</c>.
    /// <c>es-MX</c>: <c>ex-MX</c>, <c>es</c>, <c>en-US</c>.
    /// </example>
    private List<string> GetCompatibleLanguages
    (
        string language
    )
    {
        language = NormalizeLanguage(language);

        List<string> locales = [language];

        if (!string.IsNullOrEmpty(_subLanguageSeparator))
        {
            while (language.Contains(_subLanguageSeparator))
            {
                int idx = language.LastIndexOf(_subLanguageSeparator);

                if (idx != -1)
                {
                    // Get part of the string before last underscore.
                    language = language[..idx];

                    locales.Add(language);
                }
            }
        }

        string defaultLanguage = NormalizeLanguage(_defaultLanguage);

        if (!locales.Contains(defaultLanguage))
        {
            locales.Add(NormalizeLanguage(defaultLanguage));
        }

        return locales;
    }

    /// <summary>
    /// Removes dashes and underscores from the language code.
    /// </summary>
    /// <param name="value">
    /// The original language code.
    /// </param>
    /// <returns>
    /// The compacted language code value.
    /// </returns>
    private static string Compact
    (
        string value
    )
    {
        return value
            .Replace("-", "")
            .Replace("_", "");
    }

    /// <summary>
    /// Generates the cache key for the specified language code and email template..
    /// </summary>
    /// <param name="templateId">
    /// The template identifier.
    /// </param>
    /// <param name="language">
    /// The language code.
    /// </param>
    /// <returns>
    /// The cache key.
    /// </returns>
    private string FormatKey
    (
        string templateId,
        string language
    )
    {
        language = Compact(NormalizeLanguage(language)).ToUpper();

        string idValue = Compact(templateId).ToUpper();

        return $"{idValue}{language}";
    }

    /// <summary>
    /// Generates the path to the email template file.
    /// </summary>
    /// <param name="templateFolderPath">
    /// The template folder path.
    /// </param>
    /// <param name="templateId">
    /// The template identifier.
    /// </param>
    /// <param name="language">
    /// The language code.
    /// </param>
    /// <param name="extension">
    /// The file extension.
    /// </param>
    /// <returns>
    /// The file path.
    /// </returns>
    private string FormatPath
    (
        string templateFolderPath,
        string templateId,
        string language,
        string extension
    )
    {
        string fileName = FormatFileNameWithExtension(templateId, language, extension);

        while (templateFolderPath.EndsWith('/'))
        {
            templateFolderPath = templateFolderPath.TrimEnd('/');
        }

        while (templateFolderPath.EndsWith('\\'))
        {
            templateFolderPath = templateFolderPath.TrimEnd('\\');
        }

        return Path.GetFullPath(Path.Combine(templateFolderPath, fileName));
    }

    /// <summary>
    /// Formats the name of the template file without the extension.
    /// </summary>
    /// <param name="templateId">
    /// The template identifier.
    /// </param>
    /// <param name="language">
    /// The language code.
    /// </param>
    /// <returns>
    /// The file name in the format <c>templateId_languageCode</c>,
    /// such as <c>EmailVerification_en-us</c>.
    /// </returns>
    private string FormatFileName
    (
        string templateId,
        string language
    )
    {
        return string.IsNullOrEmpty(language) 
            ? templateId 
            : $"{templateId}{_languageSeparator}{language.ToLower()}";
    }

    /// <summary>
    /// Formats the name of the template file.
    /// </summary>
    /// <param name="templateId">
    /// The template identifier.
    /// </param>
    /// <param name="language">
    /// The language code.
    /// </param>
    /// <param name="extension">
    /// The file extension.
    /// </param>
    /// <returns>
    /// The file name in the format: TEMPLATEID-LANGUAGE_CODE.EXTENSION
    /// </returns>
    private string FormatFileNameWithExtension
    (
        string templateId,
        string language,
        string extension
    )
    {
        string? fileName = string.IsNullOrEmpty(language) ? templateId : FormatFileName(templateId, language);

#pragma warning disable IDE0011 // Add braces
        if (string.IsNullOrEmpty(extension))
            return fileName ?? "";
#pragma warning restore IDE0011 // Add braces

        return $"{fileName}{extension}";
    }

    /// <summary>
    /// Converts the language code with dashes to the language file name suffix 
    /// with underscores and in lower case.
    /// </summary>
    /// <param name="language">
    /// The language code, such as 'es-MX'.
    /// </param>
    /// <returns>
    /// The modified language code, such as 'es_mx'.
    /// </returns>
    /// <remarks>
    /// Converting the language code to lower case is important
    /// for case sensitive file systems, such as in Linux.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "<Pending>")]
    private string NormalizeLanguage
    (
        string? language = null
    )
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            language = _defaultLanguage;
        }

        if (language == null)
        {
            return "";
        }

        return !string.IsNullOrEmpty(_languageSeparator) &&
               !string.IsNullOrEmpty(_subLanguageSeparator) &&
               !string.IsNullOrEmpty(language)
            ? language.ToLower().Replace(_languageSeparator, _subLanguageSeparator)
            : language.ToLower();
    }

    /// <summary>
    /// Initializes the Razor engine.
    /// </summary>
    private static void InitializeRazor()
    {
        lock (_razorLock)
        {
            if (_razorEngine == null)
            {
                try
                {
                    _razorEngine = new RazorLightEngineBuilder()
                        .UseNoProject()
                        .UseMemoryCachingProvider()
                        .Build();
                }
                catch (Exception ex)
                {
                    throw new Exception("Cannot initialize Razor engine: RazorLightEngineBuilder.UseNoProject().UseMemoryCachingProvider().Build() failed.", ex);
                }
            }
        }
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex RegexRepeatedSpaceChars();
    #endregion
}

