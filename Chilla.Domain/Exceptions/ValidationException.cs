namespace Chilla.Domain.Exceptions;

public class OtpValidationException : Exception
{
    public OtpValidationException(string message) : base(message) { }
}