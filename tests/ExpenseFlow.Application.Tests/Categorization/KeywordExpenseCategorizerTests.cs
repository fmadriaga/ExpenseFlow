using ExpenseFlow.Application.Categorization;
using ExpenseFlow.Application.Options;
using ExpenseFlow.Domain.Entities;
using Xunit;

namespace ExpenseFlow.Application.Tests.Categorization;

public sealed class KeywordExpenseCategorizerTests
{
    private static KeywordExpenseCategorizer Create(params (string category, string[] keywords)[] rules)
    {
        var dict = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var (category, keywords) in rules)
        {
            dict[category] = keywords;
        }

        var opts = Microsoft.Extensions.Options.Options.Create(new CategoryOptions { Rules = dict });
        return new KeywordExpenseCategorizer(opts);
    }

    [Fact]
    public void Keyword_match_returns_that_category()
    {
        var sut = Create(("combustible", new[] { "ypf", "shell" }));
        var doc = new Document { MerchantName = "Estación YPF Norte" };
        Assert.Equal("combustible", sut.Categorize(doc));
    }

    [Fact]
    public void Substring_match_is_case_insensitive()
    {
        var sut = Create(("supermercado", new[] { "walmart" }));
        Assert.Equal(
            "supermercado",
            sut.Categorize(new Document { MerchantName = "Walmart Express 123" }));
        Assert.Equal(
            "supermercado",
            sut.Categorize(new Document { MerchantName = "Tienda walmart local" }));
    }

    [Fact]
    public void No_keyword_match_returns_otros()
    {
        var sut = Create(("farmacia", new[] { "farma" }));
        Assert.Equal(KeywordExpenseCategorizer.DefaultCategory, sut.Categorize(new Document { MerchantName = "Unknown Shop 999" }));
    }

    [Fact]
    public void Null_or_empty_merchant_returns_otros()
    {
        var sut = Create(("supermercado", new[] { "walmart" }));
        Assert.Equal(
            KeywordExpenseCategorizer.DefaultCategory,
            sut.Categorize(new Document { MerchantName = null }));
        Assert.Equal(
            KeywordExpenseCategorizer.DefaultCategory,
            sut.Categorize(new Document { MerchantName = "" }));
        Assert.Equal(
            KeywordExpenseCategorizer.DefaultCategory,
            sut.Categorize(new Document { MerchantName = "   " }));
    }

    [Fact]
    public void Empty_rules_returns_otros()
    {
        var sut = new KeywordExpenseCategorizer(
            Microsoft.Extensions.Options.Options.Create(new CategoryOptions()));
        Assert.Equal(
            KeywordExpenseCategorizer.DefaultCategory,
            sut.Categorize(new Document { MerchantName = "Carrefour" }));
    }
}
