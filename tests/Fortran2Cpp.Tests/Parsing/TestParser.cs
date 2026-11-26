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
}