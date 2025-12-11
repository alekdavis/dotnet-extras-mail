namespace DotNetExtras.Mail;
/// <summary>
/// Defines lists of email message addressees.
/// </summary>
public interface IMailRecipients
{
    /// <summary>
    /// Email To addresses.
    /// </summary>
    List<string>? To { get; }

    /// <summary>
    /// Email CC addresses.
    /// </summary>
    List<string>? Cc { get; }

    /// <summary>
    /// Email BCC addresses.
    /// </summary>
    List<string>? Bcc { get; }
}

