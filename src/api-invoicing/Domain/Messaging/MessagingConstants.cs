namespace Domain.Messaging;

public static class MessagingConstants
{
    public static class Exchanges
    {
        public const string Direct = "app.direct";
        public const string Events = "app.events";
    }

    public static class RoutingKeys
    {
        // Invoicing Events
        public const string InvoiceCreated = "invoice.created";
        public const string InvoicePrintConfirmed = "invoice.print-confirmed";
        public const string InvoiceCanceled = "invoice.canceled";

        // Inventory Events
        public const string InventoryReserved = "inventory.stock-reserved";
        public const string InventoryReservationFailed = "inventory.stock-reservation-failed";
        public const string InventoryConfirmed = "inventory.stock-confirmed";
        public const string InventoryConfirmationFailed = "inventory.stock-confirmation-failed";
        public const string InventoryReservedCanceled = "inventory.stock-reservation-canceled";
    }

    public static class Queues
    {
        // Inventory Consumer Queues
        public const string InventoryReserveQueue = "inventory.reserve-stock.queue";
        public const string InventoryConfirmQueue = "inventory.confirm-stock.queue";
        public const string InventoryReleaseQueue = "inventory.release-stock.queue";

        // Invoicing Consumer Queue
        public const string InvoiceStatusQueue = "invoicing.inventory-status-update.queue";
    }
}
