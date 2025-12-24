using Chilla.Domain.Aggregates.PlanAggregate;
using Chilla.Domain.Common;
using MediatR;

namespace Chilla.Application.Features.Plans.Commands.UpdatePlan;

public record UpdatePlanCommand(
    Guid Id,
    string Title,
    string Description,
    decimal Price,
    bool IsActive
) : IRequest;

public class UpdatePlanCommandHandler : IRequestHandler<UpdatePlanCommand>
{
    private readonly IPlanRepository _planRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePlanCommandHandler(IPlanRepository planRepository, IUnitOfWork unitOfWork)
    {
        _planRepository = planRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(UpdatePlanCommand request, CancellationToken cancellationToken)
    {
        var plan = await _planRepository.GetByIdAsync(request.Id, cancellationToken);
        if (plan == null) throw new NotFoundException(nameof(Plan), request.Id);

        // متد Update در دامنه باید پیاده‌سازی شود، یا پراپرتی‌ها اینجا ست شوند
        // بهتر است یک متد در Domain Entity باشد: plan.UpdateDetails(...)
        // فعلا به صورت مستقیم ست می‌کنیم (اگر ستترها Public/Internal باشند) یا از طریق Reflection/Method
        
        // فرض: متد Update در پلن وجود دارد (اگر نیست باید اضافه کنید)
        /* plan.Update(request.Title, request.Description, request.Price, request.IsActive);
        */
        
        // پیاده‌سازی موقت با فرض دسترسی (در عمل باید متد در دامین باشد):
        var type = typeof(Plan);
        type.GetProperty(nameof(Plan.Title))?.SetValue(plan, request.Title);
        type.GetProperty(nameof(Plan.Description))?.SetValue(plan, request.Description);
        type.GetProperty(nameof(Plan.Price))?.SetValue(plan, request.Price);
        type.GetProperty(nameof(Plan.IsActive))?.SetValue(plan, request.IsActive);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}