namespace DotNetExtras.Mail;
/// <summary>
/// Defines basic email message properties.
/// </summary>
public interface IMailMessage
{
    /// <summary>
    /// Returns the language code of the mail message.
    /// </summary>
    string? Language { get; }

    /// <summary>
    /// Returns the subject of the mail message.
    /// </summary>
    string? Subject { get; }

    /// <summary>
    /// Returns the body of the mail message.
    /// </summary>
    string? Body { get; }
}

