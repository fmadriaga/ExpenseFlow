using ExpenseFlow.Application.Export;
using Xunit;

namespace ExpenseFlow.Application.Tests.Export;

public sealed class DocumentCsvExporterTests
{
    [Fact]
    public void EscapeField_wraps_comma_in_quotes()
    {
        Assert.Equal(
            "\"A, B\"",
            DocumentCsvExporter.EscapeField("A, B", ','));
    }

    [Fact]
    public void EscapeField_doubles_internal_quotes()
    {
        Assert.Equal(
            "\"He said \"\"hi\"\"\"",
            DocumentCsvExporter.EscapeField("He said \"hi\"", ','));
    }

    [Fact]
    public void EscapeField_semicolon_delimiter_does_not_quote_if_no_special()
    {
        Assert.Equal("YPF", DocumentCsvExporter.EscapeField("YPF", ';'));
    }
}
