namespace Fortran2Cpp.Core.Ast;

/// <summary>
/// Pozicija u ORIGINALNOM izvornom fajlu (posle mapiranja kroz SourceMap,
/// dakle ne u spojenom/preprocesiranom tekstu). Linije i kolone počinju od 1.
/// </summary>
public readonly record struct SourceSpan(
    string File,
    int StartLine,
    int StartColumn,
    int EndLine,
    int EndColumn)
{
    /// <summary>Za čvorove koje alat sam pravi (npr. podrazumevana donja granica niza).</summary>
    public static readonly SourceSpan None = new("<none>", 0, 0, 0, 0);

    public bool IsNone => StartLine == 0;

    public override string ToString() => $"{File}:{StartLine}:{StartColumn}";
}
