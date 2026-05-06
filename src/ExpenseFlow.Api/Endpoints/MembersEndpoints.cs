using ExpenseFlow.Application.Ocr;
using ExpenseFlow.Application.Splitting;
using ExpenseFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Api.Endpoints;

public static class MembersEndpoints
{
    public static IEndpointRouteBuilder MapMembersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/members").WithTags("Members");

        group.MapGet(
            "/{memberId:int}/balance",
            async Task<IResult> (
                int memberId,
                ExpenseFlowDbContext db,
                DateOnly from,
                DateOnly to,
                int familyId = 1,
                CancellationToken cancellationToken = default) =>
            {
                if (from > to)
                {
                    return Results.BadRequest(new { error = "'from' no puede ser posterior a 'to'." });
                }

                var familyExists = await db.Families.AsNoTracking()
                    .AnyAsync(f => f.Id == familyId, cancellationToken)
                    .ConfigureAwait(false);
                if (!familyExists)
                {
                    return Results.NotFound(new { error = "Familia no encontrada", familyId });
                }

                var member = await db.FamilyMembers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        m => m.Id == memberId && m.FamilyId == familyId,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (member is null)
                {
                    return Results.NotFound(new { error = "Miembro no encontrado", memberId });
                }

                var documents = await db.Documents
                    .AsNoTracking()
                    .Include(d => d.ExpenseSplits)
                    .Where(d => d.FamilyId == familyId)
                    .Where(d => d.OcrStatus == ReceiptOcrStatuses.Success)
                    .Where(d => d.TransactionDate != null && d.TransactionDate >= from && d.TransactionDate <= to)
                    .Where(d => d.ExpenseSplits.Any())
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);

                var inputs = new List<MemberPeriodBalanceInput>(documents.Count);
                foreach (var d in documents)
                {
                    inputs.Add(
                        new MemberPeriodBalanceInput
                        {
                            DocumentId = d.Id,
                            TotalAmount = d.TotalAmount,
                            PaidByFamilyMemberId = d.PaidByFamilyMemberId,
                            Splits = d.ExpenseSplits
                                .Select(
                                    s => new MemberPeriodSplitLine
                                    {
                                        FamilyMemberId = s.FamilyMemberId,
                                        Percentage = s.Percentage,
                                    })
                                .ToList(),
                        });
                }

                var result = MemberPeriodBalanceCalculator.Compute(
                    member.Id,
                    member.Name,
                    from,
                    to,
                    inputs);
                return Results.Ok(result);
            })
            .WithSummary("Get member balance")
            .WithDescription(
                "Returns the net period balance for a family member across documents with expense splits.");

        return app;
    }
}
