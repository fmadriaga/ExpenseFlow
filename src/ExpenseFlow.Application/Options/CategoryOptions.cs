namespace ExpenseFlow.Application.Options;

/// <summary>
/// Reglas de categorización: nombre de categoría → palabras clave en <see cref="ExpenseFlow.Domain.Entities.Document.MerchantName"/>.
/// Se enlaza desde la sección <see cref="SectionName"/> (contenido = diccionario en JSON).
/// </summary>
public sealed class CategoryOptions
{
    public const string SectionName = "CategoryRules";

    /// <summary>
    /// Clave = categoría devuelta; valor = subcadenas a buscar (sin distinguir mayúsculas).
    /// </summary>
    public Dictionary<string, string[]> Rules { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
