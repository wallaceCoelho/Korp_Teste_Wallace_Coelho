using Application.Common.Interfaces;
using Application.Features.Invoices.PrintInvoice;
using Application.Tests.Helpers;
using Domain.Entities;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Application.Tests.Features;

public class PrintInvoiceHandlerTests
{
    private readonly IUnitWork _unitWork;
    private readonly PrintInvoiceHandler _handler;

    public PrintInvoiceHandlerTests()
    {
        _unitWork = Substitute.For<IUnitWork>();
        _handler = new PrintInvoiceHandler(_unitWork);
    }

    [Fact]
    public async Task Handle_WhenInvoiceNotFound_ShouldReturnNotFoundApiError()
    {
        // Arrange
        var command = new PrintInvoiceCommand(Guid.NewGuid());
        var emptyInvoices = new List<Invoice>().BuildMockDbSet();
        _unitWork.AsQueryable<Invoice>().Returns(emptyInvoices);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.IsApiError.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenInvoiceIsPending_ShouldReturnConflictApiError()
    {
        // Arrange
        var itemResult = InvoiceItem.Create(Guid.NewGuid(), "PRD-001", "Notebook", 2, 1500m);
        var invoiceResult = Invoice.Create([itemResult.Value!]);
        var invoice = invoiceResult.Value!;

        var invoices = new List<Invoice> { invoice }.BuildMockDbSet();
        _unitWork.AsQueryable<Invoice>().Returns(invoices);

        var command = new PrintInvoiceCommand(invoice.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.IsApiError.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenInvoiceIsOpen_ShouldAddPrintConfirmedEventToOutbox()
    {
        // Arrange
        var itemResult = InvoiceItem.Create(Guid.NewGuid(), "PRD-001", "Notebook", 2, 1500m);
        var invoiceResult = Invoice.Create([itemResult.Value!]);
        var invoice = invoiceResult.Value!;
        invoice.Open(); // Transition to Open

        var invoices = new List<Invoice> { invoice }.BuildMockDbSet();
        _unitWork.AsQueryable<Invoice>().Returns(invoices);

        _unitWork.ExecuteInTransactionAsync(Arg.Any<Func<CancellationToken, Task<Guid>>>(), Arg.Any<System.Data.IsolationLevel>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var func = callInfo.Arg<Func<CancellationToken, Task<Guid>>>();
                return func(CancellationToken.None);
            });

        var command = new PrintInvoiceCommand(invoice.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Match(id => id, _ => Guid.Empty, _ => Guid.Empty).ShouldBe(invoice.Id);
        await _unitWork.Received(1).AddAsync(Arg.Any<OutboxMessage>(), Arg.Any<CancellationToken>());
    }
}
