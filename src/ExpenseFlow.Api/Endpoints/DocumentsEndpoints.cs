using System.Text;
using ExpenseFlow.Application.DTOs;
using ExpenseFlow.Application.Abstractions;
using ExpenseFlow.Application.Export;
using ExpenseFlow.Application.Ocr;
using ExpenseFlow.Application.Splitting;
using ExpenseFlow.Domain.Entities;
using ExpenseFlow.Infrastructure.Configuration;
using ExpenseFlow.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
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
                int familyId = 1,
                DateOnly? from = null,
                DateOnly? to = null,
                string? status = null,
                string? category = null) =>
            {
                if (!await FamilyExistsAsync(db, familyId, cancellationToken).ConfigureAwait(false))
                {
                    return Results.NotFound(new { error = "Familia no encontrada", familyId });
                }

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

                var query = ApplyDocumentFilters(db.Documents.AsNoTracking(), familyId, from, to, status);
                if (!string.IsNullOrWhiteSpace(category))
                {
                    var cat = category.Trim();
                    query = query.Where(d => d.Category == cat);
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
                        Category = d.Category,
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
            })
            .WithSummary("List documents")
            .WithDescription(
                "Paginated list of processed documents. Supports filters: familyId, from, to, status, category.");

        group.MapGet(
            "/recent",
            async Task<IResult> (
                ExpenseFlowDbContext db,
                CancellationToken cancellationToken,
                int limit = 20,
                int familyId = 1) =>
            {
                if (!await FamilyExistsAsync(db, familyId, cancellationToken).ConfigureAwait(false))
                {
                    return Results.NotFound(new { error = "Familia no encontrada", familyId });
                }

                if (limit < 1)
                {
                    return Results.BadRequest("limit must be >= 1");
                }

                if (limit > 50)
                {
                    limit = 50;
                }

                var items = await ApplyDocumentFilters(db.Documents.AsNoTracking(), familyId, null, null, null)
                    .OrderByDescending(d => d.Id)
                    .Take(limit)
                    .Select(d => new DocumentSummaryDto
                    {
                        Id = d.Id,
                        MerchantName = d.MerchantName,
                        TransactionDate = d.TransactionDate,
                        TotalAmount = d.TotalAmount,
                        Currency = d.Currency,
                        Category = d.Category,
                        OcrStatus = d.OcrStatus,
                        Confidence = d.Confidence,
                        CreatedAt = d.CreatedAt,
                    })
                    .ToListAsync(cancellationToken);

                return Results.Ok(items);
            })
            .WithSummary("Get recent documents")
            .WithDescription(
                "Returns the N most recent documents for the mobile history feed. Default limit: 20, max: 50.");

        group.MapGet(
            "/{id:int}",
            async Task<IResult> (int id, ExpenseFlowDbContext db, int familyId = 1, CancellationToken cancellationToken = default) =>
            {
                if (!await FamilyExistsAsync(db, familyId, cancellationToken).ConfigureAwait(false))
                {
                    return Results.NotFound(new { error = "Familia no encontrada", familyId });
                }

                var document = await db.Documents
                    .AsNoTracking()
                    .Include(d => d.DocumentLines)
                    .FirstOrDefaultAsync(
                        d => d.Id == id && d.FamilyId == familyId,
                        cancellationToken);
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
                    Category = document.Category,
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
            })
            .WithSummary("Get document detail")
            .WithDescription("Returns full document including DocumentLines and raw OCR JSON.");

        group.MapPatch(
            "/{id:int}",
            async Task<IResult> (
                int id,
                PatchDocumentRequestDto body,
                ExpenseFlowDbContext db,
                int familyId = 1,
                CancellationToken cancellationToken = default) =>
            {
                if (!await FamilyExistsAsync(db, familyId, cancellationToken).ConfigureAwait(false))
                {
                    return Results.NotFound(new { error = "Familia no encontrada", familyId });
                }

                var document = await db.Documents
                    .FirstOrDefaultAsync(d => d.Id == id && d.FamilyId == familyId, cancellationToken);
                if (document is null)
                {
                    return Results.NotFound(new
                    {
                        error = "Documento no encontrado",
                        id,
                    });
                }

                if (body.MerchantName is not null)
                {
                    document.MerchantName = body.MerchantName;
                }

                if (body.TransactionDate.HasValue)
                {
                    document.TransactionDate = body.TransactionDate;
                }

                if (body.TotalAmount.HasValue)
                {
                    document.TotalAmount = body.TotalAmount;
                }

                if (body.Category is not null)
                {
                    document.Category = body.Category;
                }

                await db.SaveChangesAsync(cancellationToken);
                return Results.NoContent();
            })
            .WithSummary("Patch document")
            .WithDescription(
                "Partially updates editable fields: merchant name, transaction date, total amount, category.");

        group.MapGet(
            "/export",
            async Task (
                HttpContext http,
                ExpenseFlowDbContext db,
                ICsvExporter exporter,
                int familyId = 1,
                DateOnly? from = null,
                DateOnly? to = null,
                string? status = null,
                string? delimiter = null,
                CancellationToken cancellationToken = default) =>
            {
                if (!await FamilyExistsAsync(db, familyId, cancellationToken).ConfigureAwait(false))
                {
                    http.Response.StatusCode = StatusCodes.Status404NotFound;
                    await http.Response.WriteAsJsonAsync(
                        new { error = "Familia no encontrada", familyId },
                        cancellationToken).ConfigureAwait(false);
                    return;
                }

                var sep = ResolveExportDelimiter(delimiter);
                http.Response.ContentType = "text/csv; charset=utf-8";
                http.Response.Headers.ContentDisposition = @"attachment; filename=""documents.csv""";

                var query = ApplyDocumentFilters(db.Documents.AsNoTracking(), familyId, from, to, status)
                    .OrderByDescending(d => d.Id)
                    .Select(d => new DocumentExportRow(
                        d.Id,
                        d.FilePath,
                        d.MerchantName,
                        d.TransactionDate,
                        d.TotalAmount,
                        d.TaxAmount,
                        d.Currency,
                        d.Category,
                        d.OcrStatus,
                        d.CreatedAt));

                await using var writer = new StreamWriter(
                    http.Response.Body,
                    new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
                    bufferSize: 65536,
                    leaveOpen: true);
                await exporter
                    .WriteDocumentExportAsync(query.AsAsyncEnumerable(), writer, sep, cancellationToken)
                    .ConfigureAwait(false);
            })
            .WithSummary("Export documents as CSV")
            .WithDescription(
                "Downloads a UTF-8 CSV of documents matching the given filters. Use delimiter=semicolon for European locale.");

        group.MapPost(
            "/{id:int}/reprocess",
            async Task<IResult> (
                int id,
                ExpenseFlowDbContext db,
                IFileRestorer restorer,
                IWebHostEnvironment hostEnvironment,
                ILoggerFactory loggerFactory,
                int familyId = 1,
                CancellationToken cancellationToken = default) =>
            {
                if (!await FamilyExistsAsync(db, familyId, cancellationToken).ConfigureAwait(false))
                {
                    return Results.NotFound(new { error = "Familia no encontrada", familyId });
                }

                var logger = loggerFactory.CreateLogger("DocumentsEndpoints");
                var document = await db.Documents
                    .Include(d => d.Family)
                    .FirstOrDefaultAsync(d => d.Id == id && d.FamilyId == familyId, cancellationToken);
                if (document is null)
                {
                    return Results.NotFound(new
                    {
                        error = "Documento no encontrado",
                        id,
                    });
                }

                if (string.Equals(document.OcrStatus, ReceiptOcrStatuses.Success, StringComparison.Ordinal))
                {
                    return Results.Json(
                        new { error = "El documento ya se procesó correctamente; no requiere reproceso.", id },
                        statusCode: StatusCodes.Status422UnprocessableEntity);
                }

                document.OcrStatus = ReceiptOcrStatuses.Pending;
                document.ErrorMessage = null;
                await db.SaveChangesAsync(cancellationToken);

                if (string.IsNullOrWhiteSpace(document.FileHash))
                {
                    logger.LogWarning(
                        "Reprocess id={DocumentId}: Pending sin FileHash; no se puede buscar en error.",
                        id);
                }
                else
                {
                    var errorRoot = ContentRootPathResolver.Resolve(
                        hostEnvironment.ContentRootPath,
                        document.Family.ErrorPath);
                    var inboxRoot = ContentRootPathResolver.Resolve(
                        hostEnvironment.ContentRootPath,
                        document.Family.InboxPath);
                    var sourcePath = await restorer
                        .FindSourcePathInErrorTreeAsync(document.FileHash, errorRoot, cancellationToken)
                        .ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(sourcePath))
                    {
                        await restorer
                            .RestoreToInboxAsync(sourcePath, inboxRoot, cancellationToken)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        logger.LogWarning(
                            "Reprocess id={DocumentId}: actualizado a Pending; fichero no encontrado en carpeta error.",
                            id);
                    }
                }

                return Results.Ok(new { message = "Documento marcado para reproceso.", id });
            })
            .WithSummary("Reprocess document")
            .WithDescription(
                "Resets OcrStatus to Pending and moves the file back to inbox if it was in the error folder. Returns 422 if document is already successful.");

        group.MapPost(
            "/{id:int}/split",
            async Task<IResult> (
                int id,
                SetDocumentSplitRequestDto body,
                ExpenseFlowDbContext db,
                int familyId = 1,
                CancellationToken cancellationToken = default) =>
            {
                if (!await FamilyExistsAsync(db, familyId, cancellationToken).ConfigureAwait(false))
                {
                    return Results.NotFound(new { error = "Familia no encontrada", familyId });
                }

                if (body.Splits is null || body.Splits.Count == 0)
                {
                    return Results.BadRequest(new { error = "Debe indicar al menos una línea de reparto con porcentajes." });
                }

                var distinctMembers = body.Splits.Select(s => s.FamilyMemberId).Distinct().Count();
                if (distinctMembers != body.Splits.Count)
                {
                    return Results.BadRequest(new { error = "Cada miembro solo puede aparecer una vez en el reparto." });
                }

                decimal[] percentages;
                try
                {
                    percentages = body.Splits.Select(s => s.Percentage).ToArray();
                    SplitExpensePercentageValidator.EnsureSumIsHundred(percentages);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }

                var document = await db.Documents
                    .Include(d => d.ExpenseSplits)
                    .FirstOrDefaultAsync(d => d.Id == id && d.FamilyId == familyId, cancellationToken)
                    .ConfigureAwait(false);
                if (document is null)
                {
                    return Results.NotFound(new { error = "Documento no encontrado", id });
                }

                var splitMemberIds = body.Splits.Select(s => s.FamilyMemberId).ToList();
                var membersInFamily = await db.FamilyMembers
                    .AsNoTracking()
                    .Where(m => m.FamilyId == familyId && splitMemberIds.Contains(m.Id))
                    .Select(m => m.Id)
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false);
                if (membersInFamily.Count != splitMemberIds.Count)
                {
                    return Results.BadRequest(
                        new { error = "Algún miembro del reparto no pertenece a esta familia." });
                }

                var paidOk = await db.FamilyMembers
                    .AsNoTracking()
                    .AnyAsync(
                        m => m.Id == body.PaidByFamilyMemberId && m.FamilyId == familyId,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (!paidOk)
                {
                    return Results.BadRequest(new { error = "Quien pagó debe ser miembro de la familia." });
                }

                db.ExpenseSplits.RemoveRange(document.ExpenseSplits);

                foreach (var line in body.Splits)
                {
                    document.ExpenseSplits.Add(
                        new ExpenseSplit
                        {
                            FamilyMemberId = line.FamilyMemberId,
                            Percentage = Math.Round(line.Percentage, 2, MidpointRounding.AwayFromZero),
                        });
                }

                document.PaidByFamilyMemberId = body.PaidByFamilyMemberId;
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return Results.NoContent();
            })
            .WithSummary("Set expense split")
            .WithDescription(
                "Assigns who paid and sets percentage splits per family member. Split percentages must sum to 100.");

        return app;
    }

    private static Task<bool> FamilyExistsAsync(
        ExpenseFlowDbContext db,
        int familyId,
        CancellationToken cancellationToken) =>
        db.Families.AsNoTracking().AnyAsync(f => f.Id == familyId, cancellationToken);

    private static IQueryable<Document> ApplyDocumentFilters(
        IQueryable<Document> query,
        int familyId,
        DateOnly? from,
        DateOnly? to,
        string? status)
    {
        query = query.Where(d => d.FamilyId == familyId);
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

        return query;
    }

    private static char ResolveExportDelimiter(string? delimiter)
    {
        if (string.IsNullOrWhiteSpace(delimiter))
        {
            return ',';
        }

        var t = delimiter.Trim();
        if (t is ";" or "semicolon")
        {
            return ';';
        }

        if (t is "," or "comma")
        {
            return ',';
        }

        if (t.Length == 1)
        {
            if (t[0] is ',' or ';')
            {
                return t[0];
            }
        }

        return ',';
    }
}
