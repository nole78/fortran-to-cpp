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
        var text = TestParser.ParseToString("2 + 3 * 4", p => p.expr());
        _out.WriteLine(text);

        Assert.Contains("(expr (expr 2) (addOp +) (expr (expr 3) (mulOp *) (expr 4)))", text);
    }
}