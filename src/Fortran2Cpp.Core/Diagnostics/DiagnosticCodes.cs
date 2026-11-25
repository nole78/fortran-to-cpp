namespace Fortran2Cpp.Core.Diagnostics;

/// <summary>
/// Stabilne šifre dijagnostika. Šifra se nikad ne menja niti ponovo koristi;
/// nova pravila dobijaju novu šifru. Spisak se održava i u docs/analiza.md.
///
/// Opsezi:
///   F2C1xxx  preprocesiranje i parsiranje   (Anja)
///   F2C2xxx  semantika                       (Anja)
///   F2C3xxx  nepodržani konstrukti           (Anja, UnsupportedDetector)
///   F2C4xxx  generisanje koda                (David)
///   F2C5xxx  verifikacija                    (David)
///   F2C9xxx  greške u samom alatu
/// </summary>
public static class DiagnosticCodes
{
    // 1xxx: preprocesiranje i parsiranje
    public const string SyntaxError = "F2C1001";
    public const string UnterminatedContinuation = "F2C1002";

    // 2xxx: semantika
    public const string UndeclaredName = "F2C2001";        // uz IMPLICIT NONE
    public const string DuplicateDeclaration = "F2C2002";
    public const string ImplicitlyTyped = "F2C2003";       // Info: ime je dobilo implicitni tip
    public const string AssignToParameter = "F2C2004";
    public const string ArgumentCountMismatch = "F2C2005";
    public const string TypeMismatch = "F2C2006";
    public const string NonConstantParameter = "F2C2007";

    // 3xxx: nepodržano
    public const string UnsupportedStatement = "F2C3001";  // GOTO, COMMON, EQUIVALENCE, FORMAT...
    public const string UnsupportedKind = "F2C3002";       // integer(kind=8), real(8)
    public const string UnsupportedArrayShape = "F2C3003"; // a(:), a(*), rank > 2
    public const string UnsupportedIo = "F2C3004";         // WRITE(10,*), formatirani izlaz

    // 4xxx: generisanje koda
    public const string LossyConversion = "F2C4001";       // Warning: npr. real → integer dodela

    // 5xxx: verifikacija
    public const string FortranCompileFailed = "F2C5001";
    public const string CppCompileFailed = "F2C5002";
    public const string OutputMismatch = "F2C5003";
    public const string ExitCodeMismatch = "F2C5004";
    public const string Timeout = "F2C5005";

    // 9xxx: interne greške alata
    public const string InternalError = "F2C9001";
}
