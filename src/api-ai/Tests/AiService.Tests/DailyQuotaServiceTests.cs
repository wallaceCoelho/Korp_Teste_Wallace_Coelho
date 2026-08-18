using Application.Security;
using FluentAssertions;
using Xunit;

namespace AiService.Tests;

public class DailyQuotaServiceTests
{
    private readonly InMemoryDailyQuotaService _sut = new();

    [Fact]
    public void ConsumeQuota_WhenUnderLimit_ShouldAllowAndReturnCorrectRemaining()
    {
        // Arrange
        var clientId = "client-user-123";
        var maxLimit = 15;

        // Act
        var result1 = _sut.ConsumeQuota(clientId, maxLimit);
        var result2 = _sut.ConsumeQuota(clientId, maxLimit);

        // Assert
        result1.IsAllowed.Should().BeTrue();
        result1.UsedToday.Should().Be(1);
        result1.Remaining.Should().Be(14);
        result1.ErrorMessage.Should().BeNull();

        result2.IsAllowed.Should().BeTrue();
        result2.UsedToday.Should().Be(2);
        result2.Remaining.Should().Be(13);
    }

    [Fact]
    public void ConsumeQuota_WhenExceedingLimit_ShouldBlock()
    {
        // Arrange
        var clientId = "client-spammer-999";
        var maxLimit = 3;

        // Act
        _sut.ConsumeQuota(clientId, maxLimit); // 1
        _sut.ConsumeQuota(clientId, maxLimit); // 2
        var result3 = _sut.ConsumeQuota(clientId, maxLimit); // 3 (allowed)
        var result4 = _sut.ConsumeQuota(clientId, maxLimit); // 4 (blocked)

        // Assert
        result3.IsAllowed.Should().BeTrue();
        result3.Remaining.Should().Be(0);

        result4.IsAllowed.Should().BeFalse();
        result4.Remaining.Should().Be(0);
        result4.ErrorMessage.Should().Contain("Você atingiu o limite diário de 3 gerações");
    }

    [Fact]
    public void ConsumeQuota_ForDifferentClients_ShouldTrackIndependently()
    {
        // Arrange
        var clientA = "user-a";
        var clientB = "user-b";
        var maxLimit = 5;

        // Act
        _sut.ConsumeQuota(clientA, maxLimit);
        _sut.ConsumeQuota(clientA, maxLimit);
        var resultB = _sut.ConsumeQuota(clientB, maxLimit);

        // Assert
        var statusA = _sut.GetQuotaStatus(clientA, maxLimit);
        statusA.UsedToday.Should().Be(2);
        statusA.Remaining.Should().Be(3);

        resultB.UsedToday.Should().Be(1);
        resultB.Remaining.Should().Be(4);
    }

    [Fact]
    public void GetQuotaStatus_ShouldNotIncrementUsageCount()
    {
        // Arrange
        var clientId = "user-checker";
        var maxLimit = 15;

        // Act
        _sut.ConsumeQuota(clientId, maxLimit);
        var status1 = _sut.GetQuotaStatus(clientId, maxLimit);
        var status2 = _sut.GetQuotaStatus(clientId, maxLimit);

        // Assert
        status1.UsedToday.Should().Be(1);
        status2.UsedToday.Should().Be(1);
    }
}
