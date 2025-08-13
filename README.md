# DotNetExtras.Mail

`DotNetExtras.Mail` is a .NET Core library that simplifies finding, loading, and transforming localized email templates.

Use the `DotNetExtras.Mail` library to:

- Pick the localized version of an email template for the specified language and culture.
- Find an alternative template if the specified language is not available.
- Merge localized templates with message-specific data.
- Allow localization of the email subjects along with the body text.
- Retrieve email subject from the merged template.

Notice that this library does not send emails. It only provides functionality to prepare email templates for sending.

## Usage

Let's assume that there are localized email templates for a notification identified by the template ID (`Zodiac`) and language extensions:

```
Zodiac/
  Zodiac_en-US.html
  Zodiac_en-GB.html
  Zodiac_es.html
  Zodiac_fr.html
  Zodiac_pt.html
  ...
```

The templates use the [Razor syntax](https://www.codecademy.com/learn/asp-net-i/modules/asp-net-razor-syntax/cheatsheet) and the `Model` placeholders, such as `@Model.Name` or `@Raw(Model.Name)` for text substitutions. The following code will try to load the `es-MX` version of the template and merge the specified template or the best alternative with the provided data:

```cs
// Data object to be merged with the template.
Data data = new()
{
    Zodiac = "Leo",
    Name = "John",
    Year = 2025
};

// Use the defaults in the constructor.
MailTemplate template = new();

// Load the 'es-MX' version of the 'Zodiac' email notification template 
// from the 'Samples/Zodiac' folder and merge it with the provided data.
// If the 'es-MX' version is not available, it will fall back to the 'es' translation.
// If the 'es' translation is also not available, it will fall back to the default template
// based on whatever default language suffix was defined previously.
template.Load("Samples/Zodiac", "Zodiac", "es-MX", ".html", data);
```

If the template was loaded successfully, the `template` object will contain the merged data. You can access the merged subject and body text as follows:

```cs
// The template object's 'Subject' property will hold the merged value 
// of the file template 'title' element.
string subject = template.Subject;

// The template object's 'Body' property will hold the merged value 
// of the file template's 'body' element.
string body = template.Body;

// The template object's 'Language' propoerty will hold the language code 
// actually used by the template.
string language = template.Language;

```

## Documentation

For complete documentation, usage details, and code samples, see:

- [Documentation](https://alekdavis.github.io/dotnet-extras-mail)
- [Unit tests](https://github.com/alekdavis/dotnet-extras-mail/tree/main/MailTests)

## Package

Install the latest version of the `DotNetExtras.Mail` Nuget package from:

- [https://www.nuget.org/packages/DotNetExtras.Mail](https://www.nuget.org/packages/DotNetExtras.Mail)

## Resources

To simplify the process of building localized email templates, see:

- [XslMail](https://github.com/alekdavis/xslmail)

## See also

Check out other `DotNetExtras` libraries at:

- [https://github.com/alekdavis/dotnet-extras](https://github.com/alekdavis/dotnet-extras)
