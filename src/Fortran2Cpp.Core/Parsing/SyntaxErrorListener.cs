using Antlr4.Runtime;
using Fortran2Cpp.Core.Ast;
using Fortran2Cpp.Core.Diagnostics;

namespace Fortran2Cpp.Core.Parsing;

public sealed class SyntaxErrorListener : BaseErrorListener, IAntlrErrorListener<int>
{
    private readonly DiagnosticBag _diagnostics;
    private readonly string _file;

    public SyntaxErrorListener(DiagnosticBag diagnostics, string file)
    {
        _diagnostics = diagnostics;
        _file = file;
    }

    //parsing errors
    public override void SyntaxError(
        TextWriter output, 
        IRecognizer recognizer, 
        IToken offendingSymbol, 
        int line, 
        int charPositionInLine, 
        string msg, RecognitionException e) 
        => Report(line, charPositionInLine, msg);

    //lexing errors
    public void SyntaxError(
        TextWriter output, 
        IRecognizer recognizer, 
        int offendingSymbol, 
        int line, 
        int charPositionInLine, 
        string msg, RecognitionException e) 
        => Report(line, charPositionInLine, msg);

    private void Report(int line, int charPositionInLine, string msg) 
    {
        var span = new SourceSpan(_file, line, charPositionInLine + 1, line, charPositionInLine + 1);
        _diagnostics.Error(DiagnosticCodes.SyntaxError, msg, span);
    }

}
