using Antlr4.Runtime;
using Fortran2Cpp.Core.Grammar;
using Fortran2Cpp.Core.Diagnostics;
using Fortran2Cpp.Core.Parsing;
using Xunit;
using Xunit.Abstractions;

public class SyntaxErrorListenerTests
{
    [Fact]
    public void Reports_line_and_column_of_syntax_error()
    {
        var diag = new DiagnosticBag();
        TestParser.Parse("2 + * 3", diag , p => p.expr());

        var error = Assert.Single(diag);
        Assert.Equal(DiagnosticCodes.SyntaxError, error.Code);
        Assert.Equal(1, error.Span.StartLine);
        Assert.Equal(5, error.Span.StartColumn);
    }
}