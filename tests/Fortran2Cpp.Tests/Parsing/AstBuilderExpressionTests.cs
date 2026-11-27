using Antlr4.Runtime;
using Fortran2Cpp.Core.Ast;
using Fortran2Cpp.Core.Diagnostics;
using Fortran2Cpp.Core.Grammar;
using Fortran2Cpp.Core.Parsing;
using Xunit;

namespace Fortran2Cpp.Tests.Parsing;

public class AstBuilderExpressionTests
{
    /// <summary>
    /// <c>2 + 3 * 4</c>
    /// </summary>
    [Fact]
    public void Build_AdditionWithMultiplication_ShouldRespectPrecedence()
    {
        // Arrange
        var ctx = (FortranParser.ExprOnlyContext)TestParser.ParseOk("2 + 3 * 4", p => p.exprOnly());
        var diagnostics = new DiagnosticBag();
        var builder = new AstBuilder("test.f90", diagnostics);

        // Act
        var ast = builder.Build<Expr>(ctx.expr());

        // Assert
        Assert.Empty(diagnostics);
        var add = Assert.IsType<BinaryExpr>(ast);
        Assert.Equal(BinaryOp.Add, add.Op);
        var mul = Assert.IsType<BinaryExpr>(add.Right);
        Assert.Equal(BinaryOp.Mul, mul.Op);
    }
}