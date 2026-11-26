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
        Parse("2 + * 3", diag);

        var error = Assert.Single(diag);
        Assert.Equal(DiagnosticCodes.SyntaxError, error.Code);
        Assert.Equal(1, error.Span.StartLine);
        Assert.Equal(5, error.Span.StartColumn);
    }

    //This is a helper method, used to parse the source code and collect diagnostics, as well as keep the code clean
    private static void Parse(string source, DiagnosticBag diagnostics)
    {
        var input = new AntlrInputStream(source);
        var listener = new SyntaxErrorListener(diagnostics, "<test>");

        var lexer = new FortranLexer(input);
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(listener);

        var tokens = new CommonTokenStream(lexer);
        var parser = new FortranParser(tokens);
        parser.RemoveErrorListeners();
        parser.AddErrorListener(listener);

        _ = parser.programUnit();
    }
}