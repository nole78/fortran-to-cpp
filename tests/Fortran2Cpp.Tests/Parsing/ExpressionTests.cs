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

        lexer.RemoveErrorListeners();
        parser.RemoveErrorListeners();

        var tree = parser.expr();
        var treeText = tree.ToStringTree(parser);
        _out.WriteLine(treeText);
        return treeText;
    }

    [Fact]
    public void Multiplicative_operators_associate_left_to_right()
    {
        var treeText = ParseTree("a / b * c");

        Assert.Contains("(expr (expr (expr a) (mulOp /) (expr b)) (mulOp *) (expr c))", treeText);
    }

    [Fact]
    public void Power_is_right_associative()
    {
        var treeText = ParseTree("2 ** 3 ** 2");

        Assert.Contains("(expr (expr 2) ** (expr (expr 3) ** (expr 2)))", treeText);
    }

    [Fact]
    public void Unary_minus_and_power_precedence()
    {
        var treeText = ParseTree("-a ** 2");

        Assert.Contains("(expr (addOp -) (expr (expr a) ** (expr 2)))", treeText);
    }

    [Fact]
    public void Not_has_higher_precedence_than_and()
    {
        var treeText = ParseTree(".not. a .and. b");

        Assert.Contains("(expr (expr .not. (expr a)) .and. (expr b))", treeText);
    }

    [Theory]
    [InlineData("a == b", "==")]
    [InlineData("a .eq. b", ".eq.")]
    [InlineData("a /= b", "/=")]
    [InlineData("a .ne. b", ".ne.")]
    [InlineData("a < b", "<")]
    [InlineData("a .lt. b", ".lt.")]
    [InlineData("a <= b", "<=")]
    [InlineData("a .le. b", ".le.")]
    [InlineData("a > b", ">")]
    [InlineData("a .gt. b", ".gt.")]
    [InlineData("a >= b", ">=")]
    [InlineData("a .ge. b", ".ge.")]
    public void Parses_both_relational_operator_notations(string input, string expectedOp)
    {
        var treeText = ParseTree(input);

        Assert.Contains($"(relOp {expectedOp})", treeText);
    }

    [Fact]
    public void Parses_array_slice_a_1_to_3()
    {
        var treeText = ParseTree("a(1:3)");

        Assert.Contains("(callName a)", treeText);
        Assert.Contains("(subscript (expr 1) : (expr 3))", treeText);
    }

    [Fact]
    public void Parses_array_constructor_slash_paren()
    {
        var treeText = ParseTree("(/ 1, 2 /)");

        Assert.Contains("1", treeText);
        Assert.Contains("2", treeText);
    }

    [Theory]
    [InlineData("A + B * C")]
    [InlineData("X .AND. Y .OR. Z")]
    [InlineData(".NOT. FLAG")]
    [InlineData("ARR(I + 1, J)")]
    public void Parses_uppercase_expressions(string input)
    {
        var treeText = ParseTree(input);

        Assert.False(string.IsNullOrWhiteSpace(treeText));
        Assert.Contains("(expr", treeText);
    }
}