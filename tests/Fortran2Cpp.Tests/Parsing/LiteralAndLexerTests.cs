using Antlr4.Runtime;
using Fortran2Cpp.Core.Grammar;
using Xunit;
using Xunit.Abstractions;

namespace Fortran2Cpp.Tests.Parsing;

public class LiteralAndLexerTests
{
    private readonly ITestOutputHelper _out;
    public LiteralAndLexerTests(ITestOutputHelper output) => _out = output;

    [Theory]
    [InlineData("1.0_wp")]
    [InlineData(".5_wp")]
    [InlineData("2.5e3_dp")]
    [InlineData("2d-2_rk")]
    [InlineData("42_int64")]
    public void Parses_numeric_literals_with_kind_and_exponent(string literal)
    {
        var treeText = TestParser.ParseToString(literal, p => p.exprOnly());

        Assert.Contains(literal, treeText);
    }

    [Fact]
    public void Parses_string_with_escaped_quotes_and_continuation()
    {
        const string code = "'say ''hi''' // &\n  & 'ok' ! komentar\n\n";

        var treeText = TestParser.ParseToString(code, p => p.exprOnly());

        Assert.Contains("//", treeText);
    }

    [Theory]
    [InlineData("x + 1\n")]
    [InlineData("x + 1\r\n;")]
    public void Accepts_trailing_eol_tokens(string input)
    {
        var treeText = TestParser.ParseToString(input, p => p.exprOnly());

        Assert.Contains("x", treeText);
    }
}