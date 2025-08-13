# DotNetExtras.Mail

`DotNetExtras.Mail` is a .NET Core library that simplifies finding, loading, and transforming localized email templates.

Use the `DotNetExtras.Mail` library to:

- Pick the localized version of an email template for the specified language and culture.
- Find an alternative template if the specified language is not available.
- Merge localized templates with message-specific data.
- Allow localization of the email subjects along with the body text.
- Retrieve email subject from the merged template.

Notice that this library does not send emails. It only provides functionality to prepare email templates for sending.

## Sample usage

```csharp
// Data object to be merged with the template.
Data data = new()
{
    Zodiac = "Leo",
    Name = "John",
    Year = 2025
};

MailTemplate template = new();

// Load the 'es-MX' version of the 'Zodiac' email notification template 
// from the 'Samples/Zodiac' folder and merge it with the provided data.
// If the 'es-MX' version is not available, it will fall back to the 'es' translation.
// If the 'es' translation is also not available, it will fall back to the default template
// based on whatever default language suffix was defined previously.
template.Load("Samples/Zodiac", "Zodiac", "es-MX", ".html", data);

// The template object's 'Subject' property will hold the merged value 
// of the file template 'title' element.
string subject = template.Subject;

// The template object's 'Body' property will hold the merged value 
// of thefile template's 'body' element.
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

## See also

Check out other `DotNetExtras` libraries at:

- [https://github.com/alekdavis/dotnet-extras](https://github.com/alekdavis/dotnet-extras)
