namespace LnBot.Models;

public enum InvoiceStatus
{
    Pending,
    Settled,
    Expired,
}

public enum PaymentStatus
{
    Pending,
    Processing,
    Settled,
    Failed,
}

public enum TransactionType
{
    Credit,
    Debit,
}
