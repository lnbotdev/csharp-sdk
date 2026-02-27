namespace LnBot.Exceptions;

/// <summary>
/// Base exception for all LnBot API errors.
/// </summary>
public class LnBotException : Exception
{
    public int StatusCode { get; }
    public string Body { get; }

    public LnBotException(int statusCode, string message, string body)
        : base(message)
    {
        StatusCode = statusCode;
        Body = body;
    }
}

/// <summary>Thrown when the API returns 400 Bad Request.</summary>
public sealed class BadRequestException : LnBotException
{
    public BadRequestException(string message, string body) : base(400, message, body) { }
}

/// <summary>Thrown when the API returns 401 Unauthorized.</summary>
public sealed class UnauthorizedException : LnBotException
{
    public UnauthorizedException(string message, string body) : base(401, message, body) { }
}

/// <summary>Thrown when the API returns 403 Forbidden.</summary>
public sealed class ForbiddenException : LnBotException
{
    public ForbiddenException(string message, string body) : base(403, message, body) { }
}

/// <summary>Thrown when the API returns 404 Not Found.</summary>
public sealed class NotFoundException : LnBotException
{
    public NotFoundException(string message, string body) : base(404, message, body) { }
}

/// <summary>Thrown when the API returns 409 Conflict.</summary>
public sealed class ConflictException : LnBotException
{
    public ConflictException(string message, string body) : base(409, message, body) { }
}
