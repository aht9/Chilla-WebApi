namespace Chilla.Domain.Aggregates.InvoiceAggregate;

public enum PaymentStatus
{
    Pending = 1,
    Paid = 2,
    Failed = 3,
    Canceled = 4
}