using System.Diagnostics;
using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Caching.Memory;
using Presentation.Interfaces;
using Presentation.Models;

namespace Presentation.Services;

public class VerificationService(IConfiguration configuration, EmailClient emailClient, IMemoryCache cache)
    : IVerificationService
{
    private readonly IConfiguration _configuration = configuration;
    private readonly EmailClient _emailClient = emailClient;
    private readonly IMemoryCache _cache = cache;
    private static readonly Random _random = new();
    
    public async Task<VerificationServiceResult> SendVerificationCodeAsync(SendVerificationCodeRequest request)
    {

        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email))
                return new VerificationServiceResult { Succeeded = false, Error = "Email is required" };

            var verificationCode = _random.Next(100000, 1000000).ToString();
            var subject = $"Your code is {verificationCode}";
            var plainTextContent = $@"
        Verify Your Email Address
        
        Hello, 
        To complete your verification, please enter the following code: 
        {verificationCode}
        Alternatively, you can open the verification page using the following link:
        https://domain.com/verify
        
        If you did not initiate this request, you can safely disregard this email.
        We take your privacy seriously. No further action is required if you did not initiate this request.

        Privacy Policy:
        https://domain.com/privacy
        
        © domain.com All rights reserved. 
        ";

            var htmlContent = $@"
            <!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <title>Your verification code</title>
</head>
<body style='margin: 0; padding: 32px; font-family: Inter, sans-serif; background-color: #F7F7F7; color: #1E1E20;'>
    <div style='max-width: 600px; margin: 32px auto; background: #FFFFFF; border-radius: 16px; padding: 32px;'>
        <h1 style='font-size: 32px; font-weight: 600; color: #37437D; margin-bottom: 16px; text-align: center;'>
            Verify Your Email Address
        </h1>

        <p style='font-size: 16px; color: #1E1E20; margin-bottom: 16px;'>Hello,</p>

        <p style='font-size: 16px; color: #1E1E20; margin-bottom: 24px;'>
            To complete your verification, please enter the code below or click the button to open a new page.
        </p>

        <div style='display: flex; justify-content: center; align-items: center; padding: 16px; background-color: #FCD3FE; color: #1C2346; font-size: 16px;'>
            {verificationCode}
        </div>

        <div style='text-align: center; margin-bottom: 32px;'>
            <a href='https://domain.com/verify?email={request.Email}&token=' style='background-color:#F26CF9; color:#FFFFFF; padding:12px 24px; border-radius:8px; text-decoration:none; display:inline-block; font-weight:600;'>
                Open Verification Page
            </a>
        </div>

        <p style='font-size: 12px; color: #777777; text-align: center; margin-top: 24px;'>
            If you did not initiate this request, you can safely disregard this email.<br><br>
            We take your privacy seriously. No further action is required if you did not initiate this request. For more information about how we process personal data, please see our
            <a href='https://domain.com/privacy-policy' style='color:#F26CF9; text-decoration:none;'>Privacy Policy</a>.
        </p>

        <p style='font-size: 12px; color: #777777; text-align: center; margin-top: 24px;'>
            © domain.com. All rights reserved.
        </p>
    </div>
</body>
</html>
";

            var emailMessage = new EmailMessage(
                senderAddress: _configuration["ACS:SenderAddress"],
                recipients: new EmailRecipients([new(request.Email)]),
                content: new EmailContent(subject)
                {
                    PlainText = plainTextContent,
                    Html = htmlContent
                });

            var emailSendOperation = await _emailClient.SendAsync(WaitUntil.Started, emailMessage);
            SaveVerificationCode(new SaveVerificationCodeRequest
                { Email = request.Email, Code = verificationCode, ValidFor = TimeSpan.FromMinutes(5) });
            
            return new VerificationServiceResult { Succeeded = true, Message = "Verification email sent successfully" };
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return new VerificationServiceResult { Succeeded = false, Error = "Failed to send verification email" };
        }
        
    }

    public void SaveVerificationCode(SaveVerificationCodeRequest request)
    {
        _cache.Set(request.Email.ToLowerInvariant(), request.Code, request.ValidFor);
    }

    public VerificationServiceResult VerifyVerificationCode(VerifyVerificationCodeRequest request)
    {
        var key = request.Email.ToLowerInvariant();

        if (_cache.TryGetValue(key, out string? storedCode))
        {

            if (storedCode == request.Code)
            {
                _cache.Remove(key);
                return new VerificationServiceResult { Succeeded = true, Message = "Verification successful" };
            }
        }
        
        return new VerificationServiceResult { Succeeded = false, Error = "Invalid or expired verification code" };
    }
}