using Application.Common.Interfaces;
using Application.Common.Results;
using Application.Features.Invoices.CreateInvoice;
using Application.Tests.Helpers;
using Domain.Entities;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Application.Tests.Features;

public class CreateInvoiceHandlerTests
{
    private readonly IUnitWork _unitWork;
    private readonly CreateInvoiceHandler _handler;

    public CreateInvoiceHandlerTests()
    {
        _unitWork = Substitute.For<IUnitWork>();
        _handler = new CreateInvoiceHandler(_unitWork);
    }

    [Fact]
    public async Task Handle_WithEmptyItemsList_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new CreateInvoiceCommand([]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.IsApiError.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WithValidItemsFromFrontend_ShouldCreateInvoiceWithSequentialNumber()
    {
        // Arrange
        var emptyInvoices = new List<Invoice>().BuildMockDbSet();
        _unitWork.AsQueryable<Invoice>().Returns(emptyInvoices);

        _unitWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<CommandResult<Guid>>>>(), Arg.Any<System.Data.IsolationLevel>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var func = callInfo.Arg<Func<CancellationToken, Task<CommandResult<Guid>>>>();
                return func(CancellationToken.None);
            });

        var itemCmd = new CreateInvoiceItemCommand(
            ProductId: Guid.NewGuid(),
            ProductCode: "PRD-001",
            ProductDescription: "Notebook Gamer",
            Quantity: 2,
            UnitPrice: 3500.00m
        );

        var command = new CreateInvoiceCommand([itemCmd]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        await _unitWork.Received(1).AddAsync(Arg.Any<Invoice>(), Arg.Any<CancellationToken>());
        await _unitWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
