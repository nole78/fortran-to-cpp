using Antlr4.Runtime;
using Fortran2Cpp.Core.Grammar;
using Xunit;
using Xunit.Abstractions;

namespace Fortran2Cpp.Tests.Parsing;

public class ExecPartTests
{
    private readonly ITestOutputHelper _out;
    public ExecPartTests(ITestOutputHelper output) => _out = output;

    private string ParseTree(string input)
    {
        var lexer = new FortranLexer(new AntlrInputStream(input.Replace("\r\n", "\n")));
        var parser = new FortranParser(new CommonTokenStream(lexer));

        var tree = parser.programUnit();
        var treeText = tree.ToStringTree(parser);
        _out.WriteLine(treeText);
        return treeText;
    }

    [Fact]
    public void Parses_assignment_stmt()
    {
        var treeText = ParseTree("x = 1 + 2\nend\n");

        Assert.Contains("(assignmentStmt", treeText);
    }

    [Fact]
    public void Parses_call_stmt()
    {
        var treeText = ParseTree("call compute(a, b)\nend\n");

        Assert.Contains("(callStmt", treeText);
        Assert.Contains("compute", treeText);
    }

    [Fact]
    public void Parses_print_stmt()
    {
        var treeText = ParseTree("print *, x, y\nend\n");

        Assert.Contains("(printStmt", treeText);
    }

    [Fact]
    public void Parses_read_stmt()
    {
        var treeText = ParseTree("read(*, *) x\nend\n");

        Assert.Contains("(readStmt", treeText);
    }

    [Fact]
    public void Parses_write_stmt()
    {
        var treeText = ParseTree("write(*, *) 'ok'\nend\n");

        Assert.Contains("(writeStmt", treeText);
    }

    [Fact]
    public void Parses_exit_stmt()
    {
        var treeText = ParseTree("exit loop1\nend\n");

        Assert.Contains("(exitStmt", treeText);
    }

    [Fact]
    public void Parses_cycle_stmt()
    {
        var treeText = ParseTree("cycle loop1\nend\n");

        Assert.Contains("(cycleStmt", treeText);
    }

    [Fact]
    public void Parses_stop_stmt()
    {
        var treeText = ParseTree("stop\nend\n");

        Assert.Contains("(stopStmt", treeText);
    }

    [Fact]
    public void Parses_return_stmt()
    {
        var treeText = ParseTree("return\nend\n");

        Assert.Contains("(returnStmt", treeText);
    }

    [Fact]
    public void Parses_continue_stmt()
    {
        var treeText = ParseTree("continue\nend\n");

        Assert.Contains("(continueStmt", treeText);
    }

    [Fact]
    public void Parses_if_elseif_else_construct()
    {
        var input = """
            if (x > 0) then
                y = 1
            else if (x < 0) then
                y = -1
            else
                y = 0
            end if
            end

            """;

        var treeText = ParseTree(input);

        Assert.Contains("(ifConstruct", treeText);
        Assert.Contains("else if", treeText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("else", treeText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parses_single_line_logical_if()
    {
        var treeText = ParseTree("if (flag) x = 42\nend\n");

        Assert.Contains("(logicalIfStmt", treeText);
        Assert.Contains("(assignmentStmt", treeText);
    }

    [Fact]
    public void Parses_do_loop_with_step()
    {
        var input = """
            do i = 1, 10, 2
                sum = sum + i
            end do
            end

            """;

        var treeText = ParseTree(input);

        Assert.Contains("(doConstruct", treeText);
    }

    [Fact]
    public void Parses_line_continuation_with_ampersand()
    {
        var input = """
            result = 1 + &
                2 + &
                3
            end

            """;

        var treeText = ParseTree(input);

        Assert.Contains("(assignmentStmt", treeText);
        Assert.Contains("(expr 3)", treeText);
    }

    [Fact]
    public void Parses_goto_as_unsupported_stmt()
    {
        var treeText = ParseTree("goto 100\nend\n");

        Assert.Contains("(unsupportedStmt", treeText);
    }
}