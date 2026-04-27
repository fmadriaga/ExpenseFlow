using ExpenseFlow.Application.DTOs;
using ExpenseFlow.Domain.Entities;
using ExpenseFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ExpenseFlow.Api.Endpoints;

public static class DocumentsEndpoints
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public static IEndpointRouteBuilder MapDocumentsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/documents").WithTags("Documents");

        group.MapGet(
            string.Empty,
            async Task<IResult> (
                ExpenseFlowDbContext db,
                CancellationToken cancellationToken,
                int page = 1,
                int pageSize = DefaultPageSize,
                DateOnly? from = null,
                DateOnly? to = null,
                string? status = null) =>
            {
                if (page < 1)
                {
                    return Results.BadRequest("page must be >= 1");
                }

                if (pageSize < 1)
                {
                    return Results.BadRequest("pageSize must be >= 1");
                }

                if (pageSize > MaxPageSize)
                {
                    pageSize = MaxPageSize;
                }

                IQueryable<Document> query = db.Documents.AsNoTracking();

                if (from.HasValue)
                {
                    query = query.Where(d =>
                        d.TransactionDate != null && d.TransactionDate >= from.Value);
                }

                if (to.HasValue)
                {
                    query = query.Where(d =>
                        d.TransactionDate != null && d.TransactionDate <= to.Value);
                }

                if (!string.IsNullOrWhiteSpace(status))
                {
                    var s = status.Trim();
                    query = query.Where(d => d.OcrStatus == s);
                }

                var total = await query.CountAsync(cancellationToken);
                // SQLite (EF) no traduce OrderBy sobre DateTimeOffset; Id es autoincremental y alinea el orden con inserción.
                var items = await query
                    .OrderByDescending(d => d.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(d => new DocumentSummaryDto
                    {
                        Id = d.Id,
                        MerchantName = d.MerchantName,
                        TransactionDate = d.TransactionDate,
                        TotalAmount = d.TotalAmount,
                        Currency = d.Currency,
                        Category = null,
                        OcrStatus = d.OcrStatus,
                        Confidence = d.Confidence,
                        CreatedAt = d.CreatedAt,
                    })
                    .ToListAsync(cancellationToken);

                var body = new DocumentsListResponseDto
                {
                    Items = items,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = total,
                };
                return Results.Ok(body);
            });

        group.MapGet(
            "/{id:int}",
            async Task<IResult> (int id, ExpenseFlowDbContext db, CancellationToken cancellationToken) =>
            {
                var document = await db.Documents
                    .AsNoTracking()
                    .Include(d => d.DocumentLines)
                    .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
                if (document is null)
                {
                    return Results.NotFound(new
                    {
                        error = "Documento no encontrado",
                        id,
                    });
                }

                var dto = new DocumentDetailDto
                {
                    Id = document.Id,
                    FilePath = document.FilePath,
                    MerchantName = document.MerchantName,
                    TransactionDate = document.TransactionDate,
                    TotalAmount = document.TotalAmount,
                    Currency = document.Currency,
                    Category = null,
                    OcrStatus = document.OcrStatus,
                    Confidence = document.Confidence,
                    RawJson = document.RawJson,
                    ErrorMessage = document.ErrorMessage,
                    CreatedAt = document.CreatedAt,
                    Lines = document.DocumentLines
                        .OrderBy(l => l.Id)
                        .Select(l => new DocumentLineDto
                        {
                            Description = l.Description,
                            Quantity = l.Quantity,
                            UnitPrice = l.UnitPrice,
                            Amount = l.Amount,
                            Currency = l.Currency,
                        })
                        .ToList(),
                };

                return Results.Ok(dto);
            });

        return app;
    }
}
