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

        var tree = parser.expr();
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

    [Fact]
    public void Parses_empty_call_with_identifier()
    {
        var treeText = ParseTree("foo()");

        Assert.Contains("(callName foo)", treeText);
    }

    [Fact]
    public void Parses_call_with_real_keyword_as_callName()
    {
        var treeText = ParseTree("real(x)");

        Assert.Contains("(callName real)", treeText);
        Assert.Contains("(subscript (expr x))", treeText);
    }

    [Fact]
    public void Parses_multiple_subscripts_and_expressions()
    {
        var treeText = ParseTree("arr(i + 1, j * 2)");

        Assert.Contains("(callName arr)", treeText);
        Assert.Contains("(subscript (expr (expr i) (addOp +) (expr 1)))", treeText);
        Assert.Contains("(subscript (expr (expr j) (mulOp *) (expr 2)))", treeText);
    }

    [Theory]
    [InlineData("a(:)")]
    [InlineData("a(1:10)")]
    [InlineData("a(:10)")]
    [InlineData("a(1:)")]
    [InlineData("a(1:10:2)")]
    [InlineData("a(::2)")]
    [InlineData("a(1::2)")]
    [InlineData("a(:10:2)")]
    public void Parses_subscript_ranges_and_strides(string input)
    {
        var treeText = ParseTree(input);

        Assert.Contains("(subscript", treeText);
        Assert.Contains(":", treeText);
    }

    [Fact]
    public void Parses_combined_scalar_and_slice_subscripts()
    {
        var treeText = ParseTree("mat(1, 2:5, ::2)");

        Assert.Contains("(callName mat)", treeText);
        Assert.Contains("(subscript (expr 1))", treeText);
        Assert.Contains("(subscript (expr 2) : (expr 5))", treeText);
        Assert.Contains("(subscript :: (expr 2))", treeText);
    }
}