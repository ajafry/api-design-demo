using SharedKernel;

namespace SharedKernel.Tests;

public class ResultTests
{
    [Fact]
    public void Success_IsSuccess_True()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_IsSuccess_False()
    {
        var result = Result<int>.Failure("something went wrong");

        Assert.False(result.IsSuccess);
        Assert.Equal("something went wrong", result.Error);
        Assert.Equal(default, result.Value);
    }

    [Fact]
    public void NonGeneric_Success_IsSuccess_True()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
    }

    [Fact]
    public void NonGeneric_Failure_IsSuccess_False()
    {
        var result = Result.Failure("oops");

        Assert.False(result.IsSuccess);
        Assert.Equal("oops", result.Error);
    }
}
