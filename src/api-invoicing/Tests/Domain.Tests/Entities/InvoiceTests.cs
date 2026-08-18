using Domain.Entities;
using Domain.Enums;
using Domain.Tests.Fakers;
using Shouldly;
using Xunit;

namespace Domain.Tests.Entities;

public class InvoiceTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldReturnInvoiceWithInitialStatusPending()
    {
        // Arrange
        var item1 = InvoiceFaker.CreateValidItem(quantity: 2, unitPrice: 50m); // 100
        var item2 = InvoiceFaker.CreateValidItem(quantity: 1, unitPrice: 30m); // 30

        // Act
        var result = Invoice.Create([item1, item2]);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Id.ShouldNotBe(Guid.Empty);
        result.Value.Status.ShouldBe(InvoiceStatus.Pending);
        result.Value.Items.Count.ShouldBe(2);
        result.Value.TotalAmount.ShouldBe(130m);
        result.Value.PrintedAt.ShouldBeNull();
        result.Value.DeletedAt.ShouldBeNull();
        result.Value.CreatedAt.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow.AddSeconds(5));
    }

    [Fact]
    public void Create_WithEmptyItemsList_ShouldReturnFailure()
    {
        // Act
        var result = Invoice.Create([]);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("A nota fiscal deve conter pelo menos um produto.");
    }

    [Fact]
    public void Open_WhenStatusIsPending_ShouldTransitionStatusToOpen()
    {
        // Arrange
        var invoice = InvoiceFaker.CreateValidInvoice();
        invoice.Status.ShouldBe(InvoiceStatus.Pending);

        // Act
        var result = invoice.Open();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        invoice.Status.ShouldBe(InvoiceStatus.Open);
        invoice.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void Print_WhenStatusIsOpen_ShouldTransitionStatusToClosedAndSetPrintedAt()
    {
        // Arrange
        var invoice = InvoiceFaker.CreateValidInvoice();
        invoice.Open();
        invoice.Status.ShouldBe(InvoiceStatus.Open);

        // Act
        var result = invoice.Print();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        invoice.Status.ShouldBe(InvoiceStatus.Closed);
        invoice.PrintedAt.ShouldNotBeNull();
        invoice.PrintedAt.Value.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow.AddSeconds(5));
        invoice.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void SeeCanPrint_WhenStatusIsOpen_ShouldReturnSuccess()
    {
        // Arrange
        var invoice = InvoiceFaker.CreateValidInvoice();
        invoice.Open();

        // Act
        var result = invoice.SeeCanPrint();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void SeeCanPrint_WhenStatusIsPending_ShouldReturnFailure()
    {
        // Arrange
        var invoice = InvoiceFaker.CreateValidInvoice();

        // Act
        var result = invoice.SeeCanPrint();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe("Não é possível imprimir uma nota fiscal pendente.");
    }

    [Fact]
    public void Cancel_WhenStatusIsPendingOrOpen_ShouldTransitionToCanceled()
    {
        // Arrange
        var invoice = InvoiceFaker.CreateValidInvoice();

        // Act
        var result = invoice.Cancel();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        invoice.Status.ShouldBe(InvoiceStatus.Canceled);
        invoice.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void MarkAsRejected_WhenStatusIsPending_ShouldTransitionToRejected()
    {
        // Arrange
        var invoice = InvoiceFaker.CreateValidInvoice();
        const string reason = "Estoque insuficiente para os itens.";

        // Act
        var result = invoice.MarkAsRejected(reason);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        invoice.Status.ShouldBe(InvoiceStatus.Rejected);
        invoice.ReasonRejected.ShouldBe(reason);
        invoice.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public void Delete_ShouldSetDeletedAtAndUpdatedAt()
    {
        // Arrange
        var invoice = InvoiceFaker.CreateValidInvoice();

        // Act
        invoice.Delete();

        // Assert
        invoice.DeletedAt.ShouldNotBeNull();
        invoice.UpdatedAt.ShouldNotBeNull();
    }
}
