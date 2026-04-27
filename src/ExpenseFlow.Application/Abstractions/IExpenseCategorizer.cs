using ExpenseFlow.Domain.Entities;

namespace ExpenseFlow.Application.Abstractions;

/// <summary>
/// Asigna una categoría de gasto según señales del documento (p. ej. comercio).
/// No lanza excepciones; si no aplica, devuelve <c>otros</c>.
/// </summary>
public interface IExpenseCategorizer
{
    /// <param name="document">Documento con datos ya normalizados (p. ej. <see cref="Document.MerchantName"/>).</param>
    /// <returns>Nombre de categoría; nunca null.</returns>
    string Categorize(Document document);
}
