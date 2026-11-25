using System.Collections;
using Fortran2Cpp.Core.Ast;

namespace Fortran2Cpp.Core.Diagnostics;

/// <summary>
/// Zajednička kesa dijagnostika u koju sve faze upisuju. Greške u ulazu se
/// NE bacaju kao izuzeci, nego se upisuju ovde.
/// </summary>
public sealed class DiagnosticBag : IReadOnlyList<Diagnostic>
{
    private readonly List<Diagnostic> _items = [];

    public int Count => _items.Count;
    public Diagnostic this[int index] => _items[index];

    public bool HasErrors => _items.Exists(d => d.Severity == Severity.Error);
    public bool HasUnsupported => _items.Exists(d => d.Severity == Severity.Unsupported);

    /// <summary>CodeGen se pokreće samo ako nema ni grešaka ni nepodržanih konstrukata.</summary>
    public bool CanGenerate => !HasErrors && !HasUnsupported;

    public void Add(Diagnostic diagnostic) => _items.Add(diagnostic);

    public void AddRange(IEnumerable<Diagnostic> diagnostics) => _items.AddRange(diagnostics);

    public void Info(string code, string message, SourceSpan span) =>
        Add(new Diagnostic(Severity.Info, code, message, span));

    public void Warning(string code, string message, SourceSpan span) =>
        Add(new Diagnostic(Severity.Warning, code, message, span));

    public void Unsupported(string code, string message, SourceSpan span) =>
        Add(new Diagnostic(Severity.Unsupported, code, message, span));

    public void Error(string code, string message, SourceSpan span) =>
        Add(new Diagnostic(Severity.Error, code, message, span));

    /// <summary>Sortirano po fajlu i poziciji, za stabilan izveštaj i snapshot testove.</summary>
    public IEnumerable<Diagnostic> Ordered() =>
        _items
            .OrderBy(d => d.Span.File, StringComparer.Ordinal)
            .ThenBy(d => d.Span.StartLine)
            .ThenBy(d => d.Span.StartColumn)
            .ThenBy(d => d.Code, StringComparer.Ordinal);

    public IEnumerator<Diagnostic> GetEnumerator() => _items.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
