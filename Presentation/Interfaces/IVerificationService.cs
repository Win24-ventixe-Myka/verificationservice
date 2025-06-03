using Presentation.Models;

namespace Presentation.Interfaces;

public interface IVerificationService
{
    Task<VerificationServiceResult> SendVerificationCodeAsync(SendVerificationCodeRequest request);
    
    void SaveVerificationCode(SaveVerificationCodeRequest request);

    VerificationServiceResult VerifyVerificationCode(VerifyVerificationCodeRequest request);
}