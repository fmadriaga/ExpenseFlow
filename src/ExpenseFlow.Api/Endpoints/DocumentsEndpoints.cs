using System.Text;
using ExpenseFlow.Application.DTOs;
using ExpenseFlow.Application.Abstractions;
using ExpenseFlow.Application.Export;
using ExpenseFlow.Application.Ocr;
using ExpenseFlow.Domain.Entities;
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

                var query = ApplyDocumentFilters(db.Documents.AsNoTracking(), from, to, status);

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
            });

        group.MapGet(
            "/export",
            async Task (
                HttpContext http,
                ExpenseFlowDbContext db,
                ICsvExporter exporter,
                DateOnly? from = null,
                DateOnly? to = null,
                string? status = null,
                string? delimiter = null,
                CancellationToken cancellationToken = default) =>
            {
                var sep = ResolveExportDelimiter(delimiter);
                http.Response.ContentType = "text/csv; charset=utf-8";
                http.Response.Headers.ContentDisposition = @"attachment; filename=""documents.csv""";

                var query = ApplyDocumentFilters(db.Documents.AsNoTracking(), from, to, status)
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
            });

        group.MapPost(
            "/{id:int}/reprocess",
            async Task<IResult> (
                int id,
                ExpenseFlowDbContext db,
                IFileRestorer restorer,
                ILoggerFactory loggerFactory,
                CancellationToken cancellationToken) =>
            {
                var logger = loggerFactory.CreateLogger("DocumentsEndpoints");
                var document = await db.Documents.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
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
                    var sourcePath = await restorer
                        .FindSourcePathInErrorTreeAsync(document.FileHash, cancellationToken)
                        .ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(sourcePath))
                    {
                        await restorer.RestoreToInboxAsync(sourcePath, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        logger.LogWarning(
                            "Reprocess id={DocumentId}: actualizado a Pending; fichero no encontrado en carpeta error.",
                            id);
                    }
                }

                return Results.Ok(new { message = "Documento marcado para reproceso.", id });
            });

        return app;
    }

    private static IQueryable<Document> ApplyDocumentFilters(
        IQueryable<Document> query,
        DateOnly? from,
        DateOnly? to,
        string? status)
    {
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
