using Chilla.Domain.Aggregates.InvoiceAggregate;

namespace Chilla.Infrastructure.Persistence.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly AppDbContext _context;

    public InvoiceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Invoice>()
                    .SingleOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<Invoice?> GetByAuthorityAsync(string authority, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Invoice>()
            .SingleOrDefaultAsync(i => i.Authority == authority, cancellationToken);
    }

    public async Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        await _context.Set<Invoice>().AddAsync(invoice, cancellationToken);
    }

    public void Update(Invoice invoice)
    {
        _context.Set<Invoice>().Update(invoice);
    }
}