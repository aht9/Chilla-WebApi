namespace Chilla.Application.Features.Invoices.DTOs;

public class InvoiceItemRawDto
{
    public Guid InvoiceId { get; set; }
    public Guid PlanId { get; set; }
    public string PlanTitle { get; set; }
}