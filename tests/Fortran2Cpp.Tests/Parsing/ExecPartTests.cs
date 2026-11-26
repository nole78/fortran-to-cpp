using Antlr4.Runtime;
using Fortran2Cpp.Core.Grammar;
using Xunit;
using Xunit.Abstractions;

namespace Fortran2Cpp.Tests.Parsing;

public class ExecPartTests
{
    private readonly ITestOutputHelper _out;
    public ExecPartTests(ITestOutputHelper output) => _out = output;

    [Fact]
    public void Parses_assignment_stmt()
    {
        var treeText = TestParser.ParseToString("x = 1 + 2\nend\n", p => p.programUnit());

        Assert.Contains("(assignmentStmt", treeText);
    }

    [Fact]
    public void Parses_call_stmt()
    {
        var treeText = TestParser.ParseToString("call compute(a, b)\nend\n", p => p.programUnit());

        Assert.Contains("(callStmt", treeText);
        Assert.Contains("compute", treeText);
    }

    [Fact]
    public void Parses_print_stmt()
    {
        var treeText = TestParser.ParseToString("print *, x, y\nend\n", p => p.programUnit());

        Assert.Contains("(printStmt", treeText);
    }

    [Fact]
    public void Parses_read_stmt()
    {
        var treeText = TestParser.ParseToString("read(*, *) x\nend\n", p => p.programUnit());

        Assert.Contains("(readStmt", treeText);
    }

    [Fact]
    public void Parses_write_stmt()
    {
        var treeText = TestParser.ParseToString("write(*, *) 'ok'\nend\n", p => p.programUnit());

        Assert.Contains("(writeStmt", treeText);
    }

    [Fact]
    public void Parses_exit_stmt()
    {
        var treeText = TestParser.ParseToString("exit loop1\nend\n", p => p.programUnit());

        Assert.Contains("(exitStmt", treeText);
    }

    [Fact]
    public void Parses_cycle_stmt()
    {
        var treeText = TestParser.ParseToString("cycle loop1\nend\n", p => p.programUnit());

        Assert.Contains("(cycleStmt", treeText);
    }

    [Fact]
    public void Parses_stop_stmt()
    {
        var treeText = TestParser.ParseToString("stop\nend\n", p => p.programUnit());

        Assert.Contains("(stopStmt", treeText);
    }

    [Fact]
    public void Parses_return_stmt()
    {
        var treeText = TestParser.ParseToString("return\nend\n", p => p.programUnit());

        Assert.Contains("(returnStmt", treeText);
    }

    [Fact]
    public void Parses_continue_stmt()
    {
        var treeText = TestParser.ParseToString("continue\nend\n", p => p.programUnit());

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

        var treeText = TestParser.ParseToString(input, p => p.programUnit());

        Assert.Contains("(ifConstruct", treeText);
        Assert.Contains("else if", treeText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("else", treeText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parses_single_line_logical_if()
    {
        var treeText = TestParser.ParseToString("if (flag) x = 42\nend\n", p => p.programUnit());

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

        var treeText = TestParser.ParseToString(input, p => p.programUnit());

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

        var treeText = TestParser.ParseToString(input, p => p.programUnit());

        Assert.Contains("(assignmentStmt", treeText);
        Assert.Contains("(expr 3)", treeText);
    }

    [Fact]
    public void Parses_goto_as_unsupported_stmt()
    {
        var treeText = TestParser.ParseToString("goto 100\nend\n", p => p.programUnit());

        Assert.Contains("(unsupportedStmt", treeText);
    }
}