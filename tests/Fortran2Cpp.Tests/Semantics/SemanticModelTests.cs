using Fortran2Cpp.Core.Ast;
using Fortran2Cpp.Core.Semantics;
using Shouldly;

namespace Fortran2Cpp.Tests.Semantics;

/// <summary>
/// Testovi za SemanticModel, SemanticModelBuilder i simbole (ponašanje samog ugovora).
/// AST i model se prave ručno, bez parsera; isti obrazac koriste CodeGen testovi.
/// </summary>
public class SemanticModelTests
{
    private static SourceSpan L(int line) => new("test.f90", line, 1, line, 1);

    /// <summary>
    /// program demo
    ///   implicit none
    ///   integer :: i
    ///   real :: a(5)
    ///   do i = 1, 5
    ///     a(i) = i ** 2
    ///   end do
    ///   print *, a(3)
    /// end program
    /// </summary>
    [Fact]
    public void Build_HandMadeDemoProgram_ShouldExposeSymbolsTypesAndKinds()
    {
        // Arrange: AST
        var five = new IntLiteral(5, "5", L(4));
        var aDecl = new EntityDecl("a", [new DimSpec(null, five, L(4))], null, null, L(4));
        var iDecl = new EntityDecl("i", [], null, null, L(3));

        var target = new NameOrCall("a", [new NameExpr("i", L(6))], L(6));
        var pow = new BinaryExpr(BinaryOp.Pow, new NameExpr("i", L(6)), new IntLiteral(2, "2", L(6)), L(6));
        var loop = new DoLoop(null, "i", new IntLiteral(1, "1", L(5)), new IntLiteral(5, "5", L(5)), null,
            [new Assignment(target, pow, L(6))], L(5));

        var printed = new NameOrCall("a", [new IntLiteral(3, "3", L(8))], L(8));
        var program = new MainProgram("demo",
            [
                new ImplicitNone(L(2)),
                new TypeDecl(new TypeSpec(BaseType.Integer, null, null, L(3)), [], [iDecl], L(3)),
                new TypeDecl(new TypeSpec(BaseType.Real, null, null, L(4)), [], [aDecl], L(4)),
            ],
            [loop, new PrintStmt([printed], L(8))],
            [],
            L(1));

        // Arrange: model
        var b = new SemanticModelBuilder();
        var unit = b.DeclareUnit(program);
        var i = b.DeclareVariable(unit, "i", FortranType.Integer, declaredAt: L(3));
        var aType = FortranType.Real.WithDims(new ArrayDim(new IntLiteral(1, "1", SourceSpan.None), five));
        var a = b.DeclareVariable(unit, "a", aType, declaredAt: L(4));

        b.Bind(iDecl, i).Bind(aDecl, a).Bind(loop, i);
        b.Bind(target, a).SetKind(target, NameOrCallKind.ArrayElement).SetType(target, FortranType.Real);
        b.Bind(pow.Left, i).SetType(pow.Left, FortranType.Integer);
        b.SetType(pow.Right, FortranType.Integer).SetConstant(pow.Right, 2L);
        b.SetType(pow, FortranType.Integer);
        b.Bind(printed, a).SetKind(printed, NameOrCallKind.ArrayElement).SetType(printed, FortranType.Real);
        b.SetConstant(five, 5L);

        // Act
        var model = b.Build();

        // Assert
        model.Units.ShouldHaveSingleItem().ShouldBeSameAs(unit);
        model.GetUnit(program).Kind.ShouldBe(UnitKind.Program);
        unit.Variables.Select(v => v.Name).ShouldBe(["i", "a"]);

        model.GetVariable(target).ShouldBeSameAs(a);
        model.GetVariable(target).Type.Rank.ShouldBe(1);
        model.GetNameOrCallKind(target).ShouldBe(NameOrCallKind.ArrayElement);

        // i ** 2 je INTEGER, a dodeljuje se REAL elementu: CodeGen ubacuje static_cast<float>
        model.GetType(pow).ShouldBe(FortranType.Integer);
        model.GetType(target).ShouldBe(FortranType.Real);

        model.GetConstantValue(model.GetVariable(target).Type.Dims[0].Upper).ShouldBe(5L);
    }

    [Fact]
    public void GetType_StructurallyEqualButUnregisteredNode_ShouldThrowInvalidOperation()
    {
        // Arrange: dva identična čvora (isti tekst, isti span); record jednakost kaže da su isti
        var registered = new NameExpr("x", L(1));
        var lookalike = new NameExpr("x", L(1));
        registered.ShouldBe(lookalike);

        // Act
        var model = new SemanticModelBuilder()
            .SetType(registered, FortranType.Integer)
            .Build();

        // Assert: model ih razlikuje po referenci
        model.GetType(registered).ShouldBe(FortranType.Integer);
        Should.Throw<InvalidOperationException>(() => model.GetType(lookalike));
    }

    [Fact]
    public void Params_DeclaredInReverseOrder_ShouldFollowHeaderOrder()
    {
        // Arrange: real function area(w, h), a deklaracije su obrnutim redom
        var fn = new Function("area", ["w", "h"], new TypeSpec(BaseType.Real, null, null, L(1)), null,
            [], [], [], L(1));
        var b = new SemanticModelBuilder();
        var unit = b.DeclareUnit(fn);
        b.DeclareVariable(unit, "area", FortranType.Real, VariableKind.FunctionResult);
        b.DeclareVariable(unit, "h", FortranType.Real, VariableKind.Dummy, Intent.In);
        b.DeclareVariable(unit, "w", FortranType.Real, VariableKind.Dummy, Intent.In);

        // Act
        var names = unit.Params.Select(p => p.Name);

        // Assert
        names.ShouldBe(["w", "h"]);
    }

    [Fact]
    public void Result_FunctionWithResultClause_ShouldReturnResultVariable()
    {
        // Arrange: real function area(w, h) result(r)
        var fn = new Function("area", ["w", "h"], null, "r", [], [], [], L(1));
        var b = new SemanticModelBuilder();
        var unit = b.DeclareUnit(fn);
        b.DeclareVariable(unit, "w", FortranType.Real, VariableKind.Dummy);
        b.DeclareVariable(unit, "h", FortranType.Real, VariableKind.Dummy);
        var r = b.DeclareVariable(unit, "r", FortranType.Real, VariableKind.FunctionResult);

        // Act
        var result = unit.Result;

        // Assert
        result.ShouldBeSameAs(r);
    }

    [Fact]
    public void IsSaved_LocalWithInitializerInFunction_ShouldBeTrue()
    {
        // Arrange: integer :: calls = 0 unutar funkcije implicitno ima SAVE
        var fn = new Function("f", [], null, null, [], [], [], L(1));
        var b = new SemanticModelBuilder();
        var unit = b.DeclareUnit(fn);

        // Act
        var calls = b.DeclareVariable(unit, "calls", FortranType.Integer,
            initializer: new IntLiteral(0, "0", L(2)));

        // Assert
        calls.IsSaved.ShouldBeTrue();
    }

    [Fact]
    public void IsSaved_LocalWithInitializerInProgram_ShouldBeFalse()
    {
        // Arrange: u glavnom programu SAVE nema efekta, pa ne treba static
        var program = new MainProgram("p", [], [], [], L(1));
        var b = new SemanticModelBuilder();
        var unit = b.DeclareUnit(program);

        // Act
        var x = b.DeclareVariable(unit, "x", FortranType.Integer,
            initializer: new IntLiteral(0, "0", L(2)));

        // Assert
        x.IsSaved.ShouldBeFalse();
    }
}
