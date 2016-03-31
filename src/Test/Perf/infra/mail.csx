using System.IO;
using System.Net.Mail;

static void SendMail(string to, string subject, string body)
{
    var server = "smtphost.redmond.corp.microsoft.com";
    var from = $"{System.Environment.UserName}@microsoft.com";
    var mailMessage = new MailMessage();
    
    mailMessage.From = new MailAddress(from);
    mailMessage.To.Add(new MailAddress(to));
    mailMessage.Subject = subject;
    mailMessage.Body = body;
    
    SmtpClient client = new SmtpClient(server);
    client.UseDefaultCredentials = true;
    client.Send(mailMessage);
}