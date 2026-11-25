using Fortran2Cpp.Core.Ast;

namespace Fortran2Cpp.Core.Diagnostics;

public enum Severity
{
    /// <summary>Informacija za izveštaj, ne utiče na rezultat.</summary>
    Info,

    /// <summary>Prevedeno, ali ponašanje može da se razlikuje (npr. implicitna konverzija).</summary>
    Warning,

    /// <summary>Konstrukt je ispravan Fortran, ali alat ga ne podržava. Taj deo se ne prevodi.</summary>
    Unsupported,

    /// <summary>Neispravan ulaz (sintaksna ili semantička greška). Prevođenje se prekida.</summary>
    Error,
}

/// <param name="Code">Stabilna šifra, npr. <c>F2C2001</c>. Spisak je u <see cref="DiagnosticCodes"/>.</param>
public sealed record Diagnostic(
    Severity Severity,
    string Code,
    string Message,
    SourceSpan Span)
{
    public override string ToString() =>
        $"{Span}: {Severity.ToString().ToLowerInvariant()} {Code}: {Message}";
}
