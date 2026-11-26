using Antlr4.Runtime;
using Fortran2Cpp.Core.Grammar;
using Fortran2Cpp.Core.Diagnostics;
using Fortran2Cpp.Core.Parsing;
using Xunit;
using Xunit.Abstractions;

public class CorpusTests
{
    public static IEnumerable<object[]> FreeFormSamples()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "samples", "free-form")))
            dir = dir.Parent;
        if (dir == null) throw new DirectoryNotFoundException("samples/free-form nije pronađen");

        var files = Directory.GetFiles(Path.Combine(dir.FullName, "samples", "free-form"), "*.f90");
        if (files.Length == 0)
            throw new InvalidOperationException($"Nema .f90 fajlova u {dir.FullName}\\samples\\free-form");

        foreach (var f in files)
            yield return new object[] { Path.GetFileName(f), f };
    }

    [Theory]
    [MemberData(nameof(FreeFormSamples))]
    public void Sample_parses_without_errors(string name, string path)
    {
        var diag = new DiagnosticBag();
        TestParser.Parse(File.ReadAllText(path), diag, p => p.compilationUnit(), name);
        Assert.True(diag.Count == 0,
            $"{name}:{Environment.NewLine}" + string.Join(Environment.NewLine, diag.Select(d => d.ToString())));
    }
}