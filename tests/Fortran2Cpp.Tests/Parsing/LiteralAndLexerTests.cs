using Antlr4.Runtime;
using Fortran2Cpp.Core.Grammar;
using Xunit;
using Xunit.Abstractions;

namespace Fortran2Cpp.Tests.Parsing;

public class LiteralAndLexerTests
{
    private readonly ITestOutputHelper _out;
    public LiteralAndLexerTests(ITestOutputHelper output) => _out = output;

    private string ParseTree(string input)
    {
        var lexer = new FortranLexer(new AntlrInputStream(input));
        var parser = new FortranParser(new CommonTokenStream(lexer));

        var tree = parser.expr();
        var treeText = tree.ToStringTree(parser);
        _out.WriteLine(treeText);
        return treeText;
    }

    [Theory]
    [InlineData("1.0_wp")]
    [InlineData(".5_wp")]
    [InlineData("2.5e3_dp")]
    [InlineData("2d-2_rk")]
    [InlineData("42_int64")]
    public void Parses_numeric_literals_with_kind_and_exponent(string literal)
    {
        var treeText = ParseTree(literal);

        Assert.Contains(literal, treeText);
    }

    [Fact]
    public void Parses_string_with_escaped_quotes_and_continuation()
    {
        const string code = "'say ''hi''' // &\n  & 'ok' ! komentar\n\n";

        var treeText = ParseTree(code);

        Assert.Contains("//", treeText);
    }

    [Theory]
    [InlineData("x + 1\n")]
    [InlineData("x + 1\r\n;")]
    public void Accepts_trailing_eol_tokens(string input)
    {
        var treeText = ParseTree(input);

        Assert.Contains("x", treeText);
    }
}