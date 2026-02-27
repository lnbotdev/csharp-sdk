using LnBot.Exceptions;
using Xunit;

namespace LnBot.Tests;

public class ExceptionTests
{
    [Fact]
    public void LnBotException_StoresStatusAndBody()
    {
        var ex = new LnBotException(500, "server error", "{\"error\":\"fail\"}");
        Assert.Equal(500, ex.StatusCode);
        Assert.Equal("server error", ex.Message);
        Assert.Equal("{\"error\":\"fail\"}", ex.Body);
        Assert.IsAssignableFrom<Exception>(ex);
    }

    [Fact]
    public void BadRequestException_HasStatus400()
    {
        var ex = new BadRequestException("bad", "body");
        Assert.Equal(400, ex.StatusCode);
        Assert.IsType<BadRequestException>(ex);
        Assert.IsAssignableFrom<LnBotException>(ex);
    }

    [Fact]
    public void UnauthorizedException_HasStatus401()
    {
        var ex = new UnauthorizedException("unauth", "body");
        Assert.Equal(401, ex.StatusCode);
    }

    [Fact]
    public void ForbiddenException_HasStatus403()
    {
        var ex = new ForbiddenException("forbidden", "body");
        Assert.Equal(403, ex.StatusCode);
    }

    [Fact]
    public void NotFoundException_HasStatus404()
    {
        var ex = new NotFoundException("not found", "body");
        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public void ConflictException_HasStatus409()
    {
        var ex = new ConflictException("conflict", "body");
        Assert.Equal(409, ex.StatusCode);
    }
}
