using Antlr4.Runtime;
using Fortran2Cpp.Core.Grammar;
using Xunit;
using Xunit.Abstractions;

namespace Fortran2Cpp.Tests.Parsing;

public class ExpressionTests
{
    private readonly ITestOutputHelper _out;
    public ExpressionTests(ITestOutputHelper output) => _out = output;

    private string ParseTree(string input)
    {
        var lexer = new FortranLexer(new AntlrInputStream(input));
        var parser = new FortranParser(new CommonTokenStream(lexer));

        var tree = parser.programUnit();
        var treeText = tree.ToStringTree(parser);
        _out.WriteLine(treeText);
        return treeText;
    }

    [Fact]
    public void Power_is_right_associative()
    {
        var treeText = ParseTree("2 ** 3 ** 2");

        Assert.Contains("(expr (expr 2) ** (expr (expr 3) ** (expr 2)))", treeText);
    }

    [Fact]
    public void Parentheses_override_precedence()
    {
        var treeText = ParseTree("(2 + 3) * 4");

        Assert.Contains("(expr ( (expr (expr 2) (addOp +) (expr 3)) )) (mulOp *) (expr 4))", treeText);
    }

    [Theory]
    [InlineData("-x + y", "(addOp -)")]
    [InlineData("a // b", "//")]
    [InlineData("a == b", "(relOp ==)")]
    [InlineData(".true. .and. .false.", ".and.")]
    [InlineData("a .eqv. b", "(eqvOp .eqv.)")]
    public void Parses_operator_expressions(string input, string expectedFragment)
    {
        var treeText = ParseTree(input);

        Assert.Contains(expectedFragment, treeText);
    }
}