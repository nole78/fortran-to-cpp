using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Fortran2Cpp.Core.Grammar;
using Fortran2Cpp.Core.Diagnostics;
using Fortran2Cpp.Core.Parsing;
using Xunit;

public static class TestParser
{
    public static IParseTree Parse(
        string text,
        DiagnosticBag diag,
        Func<FortranParser, IParseTree> rule,
        string file = "<test>")
    {
        if (!text.EndsWith('\n')) text += "\n";

        var listener = new SyntaxErrorListener(diag, file);

        var lexer = new FortranLexer(new AntlrInputStream(text));
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(listener);

        var parser = new FortranParser(new CommonTokenStream(lexer));
        parser.RemoveErrorListeners();
        parser.AddErrorListener(listener);

        return rule(parser);
    }

    public static string ParseToString(
        string text,
        Func<FortranParser, IParseTree> rule,
        string file = "<test>")
    {
        if (!text.EndsWith('\n')) text += "\n";

        var diag = new DiagnosticBag();
        var listener = new SyntaxErrorListener(diag, file);

        var lexer = new FortranLexer(new AntlrInputStream(text));
        lexer.RemoveErrorListeners();
        lexer.AddErrorListener(listener);

        var parser = new FortranParser(new CommonTokenStream(lexer));
        parser.RemoveErrorListeners();
        parser.AddErrorListener(listener);

        var tree = rule(parser);

        Assert.True(diag.Count == 0,
            string.Join(Environment.NewLine, diag.Select(d => d.ToString())));

        return Trees.ToStringTree(tree, parser);
    }

    public static IParseTree ParseOk(
        string text,
        Func<FortranParser, IParseTree> rule,
        string file = "<test>")
    {
        var diag = new DiagnosticBag();
        var tree = Parse(text, diag, rule, file);

        Assert.True(diag.Count == 0,
            string.Join(Environment.NewLine, diag.Select(d => d.ToString())));

        return tree;
    }
}