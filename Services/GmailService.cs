using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;

namespace BadmintonClub.Services;

public sealed class GmailSender
{
    private readonly GmailService _svc;

    public GmailSender(GmailService svc) => _svc = svc;

    public async Task SendAsync(string fromAddress, IEnumerable<string> toAddresses, string subject, string htmlBody, CancellationToken ct)
    {
        var mail = new MailMessage { From = new MailAddress(fromAddress), Subject = subject, IsBodyHtml = true, Body = htmlBody };
        foreach (var to in toAddresses) mail.To.Add(new MailAddress(to));
        using var mmStream = new System.IO.MemoryStream();
        using (var client = new SmtpClient())
        {
            // Tạo MIME chuẩn từ MailMessage
            var altView = AlternateView.CreateAlternateViewFromString(htmlBody, Encoding.UTF8, MediaTypeNames.Text.Html);
            mail.AlternateViews.Add(altView);
            // Dùng lớp EML helper tự xây (sau) hoặc dùng MimeKit nếu muốn.
        }
        // Tạo MIME thủ công (gọn): 
        var raw = CreateRawMime(fromAddress, toAddresses, subject, htmlBody);
        var msg = new Message { Raw = Base64UrlEncode(raw) };
        await _svc.Users.Messages.Send(msg, "me").ExecuteAsync(ct);
    }

    private static string CreateRawMime(string from, IEnumerable<string> to, string subject, string html)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"From: {from}");
        sb.AppendLine($"To: {string.Join(", ", to)}");
        sb.AppendLine($"Subject: =?utf-8?B?{Convert.ToBase64String(Encoding.UTF8.GetBytes(subject))}?=");
        sb.AppendLine("MIME-Version: 1.0");
        sb.AppendLine("Content-Type: text/html; charset=UTF-8");
        sb.AppendLine("Content-Transfer-Encoding: base64");
        sb.AppendLine();
        sb.AppendLine(Convert.ToBase64String(Encoding.UTF8.GetBytes(html)));
        return sb.ToString();
    }

    private static string Base64UrlEncode(string input)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(input)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
