using Fortran2Cpp.Core.Ast;
using Fortran2Cpp.Core.Diagnostics;
using Shouldly;

namespace Fortran2Cpp.Tests.Diagnostics;

public class DiagnosticBagTests
{
    private static SourceSpan L(int line) => new("test.f90", line, 1, line, 1);

    [Fact]
    public void CanGenerate_OnlyInfoDiagnostics_ShouldBeTrue()
    {
        // Arrange
        var bag = new DiagnosticBag();

        // Act
        bag.Info(DiagnosticCodes.ImplicitlyTyped, "'x' je implicitno REAL", L(3));

        // Assert
        bag.CanGenerate.ShouldBeTrue();
    }

    [Fact]
    public void CanGenerate_UnsupportedDiagnostic_ShouldBeFalse()
    {
        // Arrange
        var bag = new DiagnosticBag();

        // Act
        bag.Unsupported(DiagnosticCodes.UnsupportedStatement, "GOTO nije podržan", L(2));

        // Assert
        bag.CanGenerate.ShouldBeFalse();
    }

    [Fact]
    public void CanGenerate_ErrorDiagnostic_ShouldBeFalse()
    {
        // Arrange
        var bag = new DiagnosticBag();

        // Act
        bag.Error(DiagnosticCodes.SyntaxError, "očekivano ')'", L(1));

        // Assert
        bag.CanGenerate.ShouldBeFalse();
    }

    [Fact]
    public void Ordered_DiagnosticsAddedOutOfOrder_ShouldSortByLine()
    {
        // Arrange
        var bag = new DiagnosticBag();
        bag.Warning(DiagnosticCodes.LossyConversion, "real → integer", L(7));
        bag.Unsupported(DiagnosticCodes.UnsupportedStatement, "GOTO nije podržan", L(2));
        bag.Info(DiagnosticCodes.ImplicitlyTyped, "'x' je implicitno REAL", L(4));

        // Act
        var lines = bag.Ordered().Select(d => d.Span.StartLine);

        // Assert
        lines.ShouldBe([2, 4, 7]);
    }
}
