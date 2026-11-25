using Antlr4.Runtime;
using Fortran2Cpp.Core.Grammar;
using Xunit;
using Xunit.Abstractions;

public class AntlrSmokeTests
{
    private readonly ITestOutputHelper _out;
    public AntlrSmokeTests(ITestOutputHelper output) => _out = output;

    [Fact]
    public void Parses_expression_with_precedence()
    {
        var lexer  = new FortranLexer(new AntlrInputStream("2 + 3 * 4"));
        var parser = new FortranParser(new CommonTokenStream(lexer));

        var tree = parser.prog();
        _out.WriteLine(tree.ToStringTree(parser));

        Assert.Contains("(expr (expr 3) * (expr 4))", tree.ToStringTree(parser));
    }
}