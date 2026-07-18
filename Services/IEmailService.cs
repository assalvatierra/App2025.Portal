namespace Portal.Services
{
    public interface IEmailService
    {
        // send email asynchronously
        Task SendEmailAsync(string[] to, string[] cc, string[] bcc, string subject, string body);

        // read email asynchronously
        Task<string> ReadEmailAsync(string emailId);

        //read all emails asynchronously
        Task<string> ReadAllEmailsAsync();


    }
}
