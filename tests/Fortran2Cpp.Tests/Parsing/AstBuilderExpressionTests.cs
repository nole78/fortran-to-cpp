using Antlr4.Runtime;
using Fortran2Cpp.Core.Ast;
using Fortran2Cpp.Core.Grammar;
using Fortran2Cpp.Core.Parsing;
using Xunit;

namespace Fortran2Cpp.Tests.Parsing;

public class AstBuilderExpressionTests
{
    [Fact]
    public void Builds_precedence_2_plus_3_times_4()
    {
        var ctx = (FortranParser.ExprOnlyContext)TestParser.ParseOk("2 + 3 * 4", p => p.exprOnly());
        var ast = (Expr)new AstBuilder("test.f90").Visit(ctx);

        var add = Assert.IsType<BinaryExpr>(ast);
        Assert.Equal(BinaryOp.Add, add.Op);
        var mul = Assert.IsType<BinaryExpr>(add.Right);
        Assert.Equal(BinaryOp.Mul, mul.Op);
    }
}