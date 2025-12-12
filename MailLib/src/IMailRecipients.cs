namespace DotNetExtras.Mail;
/// <summary>
/// Defines lists of email message addressees.
/// </summary>
public interface IMailRecipients
{
    /// <summary>
    /// Email To addresses.
    /// </summary>
    ICollection<string>? To { get; }

    /// <summary>
    /// Email CC addresses.
    /// </summary>
    ICollection<string>? Cc { get; }

    /// <summary>
    /// Email BCC addresses.
    /// </summary>
    ICollection<string>? Bcc { get; }
}

