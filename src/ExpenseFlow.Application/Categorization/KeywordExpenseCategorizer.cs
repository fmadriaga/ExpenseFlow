using ExpenseFlow.Application.Abstractions;
using ExpenseFlow.Application.Options;
using ExpenseFlow.Domain.Entities;
using Microsoft.Extensions.Options;

namespace ExpenseFlow.Application.Categorization;

public sealed class KeywordExpenseCategorizer : IExpenseCategorizer
{
    public const string DefaultCategory = "otros";

    private readonly IOptions<CategoryOptions> _options;

    public KeywordExpenseCategorizer(IOptions<CategoryOptions> options)
    {
        _options = options;
    }

    public string Categorize(Document document)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(document.MerchantName))
            {
                return DefaultCategory;
            }

            var merchant = document.MerchantName;
            var rules = _options.Value.Rules;
            if (rules.Count == 0)
            {
                return DefaultCategory;
            }

            foreach (var category in rules.Keys.Order(StringComparer.OrdinalIgnoreCase))
            {
                if (!rules.TryGetValue(category, out var keywords) || keywords is null)
                {
                    continue;
                }

                foreach (var keyword in keywords)
                {
                    if (string.IsNullOrWhiteSpace(keyword))
                    {
                        continue;
                    }

                    if (merchant.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        return category;
                    }
                }
            }

            return DefaultCategory;
        }
        catch
        {
            return DefaultCategory;
        }
    }
}
