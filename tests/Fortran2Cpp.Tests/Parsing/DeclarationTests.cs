using Antlr4.Runtime;
using Fortran2Cpp.Core.Grammar;
using Xunit;
using Xunit.Abstractions;

namespace Fortran2Cpp.Tests.Parsing;

public class DeclarationTests
{
    private readonly ITestOutputHelper _out;
    public DeclarationTests(ITestOutputHelper output) => _out = output;

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
    public void Parses_minimal_program()
    {
        var treeText = ParseTree("end\n");

        Assert.Contains("(mainProgram", treeText);
        Assert.Contains("(endProgram", treeText);
    }

    [Fact]
    public void Parses_named_program_with_declarations()
    {
        var input = "program demo\nimplicit none\ninteger :: i, n = 10\nreal(8), parameter :: pi = 3.14159_8\nend program demo\n";
        var treeText = ParseTree(input);

        Assert.Contains("(implicitStmt", treeText);
        Assert.Contains("(declaration", treeText);
        Assert.Contains("(attr parameter)", treeText);
    }

    [Theory]
    [InlineData("integer(4) :: a\nend\n", "(typeParam (expr 4))")]
    [InlineData("real(kind=8) :: x\nend\n", "(typeParam kind = (expr 8))")]
    [InlineData("character(len=20) :: name\nend\n", "(typeParam len = (expr 20))")]
    [InlineData("character(len=*) :: msg\nend\n", "(typeParam len = *)")]
    public void Parses_type_params(string input, string expectedFragment)
    {
        var treeText = ParseTree(input);

        Assert.Contains(expectedFragment, treeText);
    }

    [Theory]
    [InlineData("real, intent(in), dimension(10) :: x, y(5)\nend\n")]
    [InlineData("integer, dimension(1:10, 0:*) :: grid\nend\n")]
    [InlineData("real, dimension(:) :: slice\nend\n")]
    public void Parses_attributes_and_dimensions(string input)
    {
        var treeText = ParseTree(input);

        Assert.Contains("(attr", treeText);
        Assert.Contains("(dimSpec", treeText);
    }

    [Fact]
    public void Parses_declaration_without_double_colon()
    {
        var treeText = ParseTree("integer a, b(3)\nend\n");

        Assert.Contains("(entityDecl a)", treeText);
        Assert.Contains("(entityDecl b ( (dimSpec (expr 3)) ))", treeText);
    }
}