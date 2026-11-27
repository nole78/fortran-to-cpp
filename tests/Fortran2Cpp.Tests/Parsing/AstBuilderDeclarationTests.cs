using Fortran2Cpp.Core.Ast;
using Fortran2Cpp.Core.Diagnostics;
using Fortran2Cpp.Core.Parsing;
using Xunit;

namespace Fortran2Cpp.Tests.Parsing;

public class AstBuilderDeclarationTests
{
    /// <summary>
    /// <c>implicit none</c>
    /// </summary>
    [Fact]
    public void Build_ImplicitNoneStatement_ShouldCreateImplicitNone()
    {
        // Arrange
        var ctx = TestParser.ParseOk("implicit none\n", p => p.implicitStmt());
        var diagnostics = new DiagnosticBag();
        var builder = new AstBuilder("test.f90", diagnostics);

        // Act
        var ast = builder.Build<ImplicitNone>(ctx);

        // Assert
        Assert.Empty(diagnostics);
        Assert.IsType<ImplicitNone>(ast);
    }

    /// <summary>
    /// <c>integer :: i, n = 10</c>
    /// </summary>
    [Fact]
    public void Build_IntegerDeclarationWithInitializer_ShouldPreserveEntities()
    {
        // Arrange
        var ctx = TestParser.ParseOk("integer :: i, n = 10\n", p => p.declaration());
        var diagnostics = new DiagnosticBag();
        var builder = new AstBuilder("test.f90", diagnostics);

        // Act
        var ast = builder.Build<TypeDecl>(ctx);

        // Assert
        Assert.Empty(diagnostics);
        Assert.Equal(BaseType.Integer, ast.Type.Base);
        Assert.Equal(2, ast.Entities.Length);
        Assert.Equal("i", ast.Entities[0].Name);
        Assert.Null(ast.Entities[0].Init);
        Assert.Equal("n", ast.Entities[1].Name);
        Assert.Equal(10L, Assert.IsType<IntLiteral>(ast.Entities[1].Init!).Value);
    }

    /// <summary>
    /// <c>real(8), parameter :: pi = 3.14159_8</c>
    /// </summary>
    [Fact]
    public void Build_ParameterDeclaration_ShouldPreserveParameterAttribute()
    {
        // Arrange
        var ctx = TestParser.ParseOk("real(8), parameter :: pi = 3.14159_8\n", p => p.declaration());
        var diagnostics = new DiagnosticBag();
        var builder = new AstBuilder("test.f90", diagnostics);

        // Act
        var ast = builder.Build<TypeDecl>(ctx);

        // Assert
        Assert.Empty(diagnostics);
        Assert.Equal(BaseType.Real, ast.Type.Base);
        Assert.Contains(ast.Attributes, attribute => attribute is ParameterAttr);
        Assert.Equal("pi", ast.Entities[0].Name);
        Assert.NotNull(ast.Entities[0].Init);
    }

    /// <summary>
    /// <c>real, intent(in) :: x</c>
    /// </summary>
    [Theory]
    [InlineData("in", Intent.In)]
    [InlineData("out", Intent.Out)]
    [InlineData("inout", Intent.InOut)]
    public void Build_IntentAttribute_ShouldMapSupportedValues(string text, Intent expected)
    {
        // Arrange
        var ctx = TestParser.ParseOk($"real, intent({text}) :: x\n", p => p.declaration());
        var diagnostics = new DiagnosticBag();
        var builder = new AstBuilder("test.f90", diagnostics);

        // Act
        var ast = builder.Build<TypeDecl>(ctx);

        // Assert
        Assert.Empty(diagnostics);
        var intent = Assert.IsType<IntentAttr>(Assert.Single(ast.Attributes));
        Assert.Equal(expected, intent.Intent);
    }

    /// <summary>
    /// <c>real, dimension(10) :: x</c>
    /// </summary>
    [Theory]
    [InlineData("dimension(10)", null, 10L)]
    [InlineData("dimension(0:9)", 0L, 9L)]
    [InlineData("dimension(:)", null, null)]
    public void Build_DimensionAttribute_ShouldPreserveBounds(
        string attributeText,
        long? expectedLower,
        long? expectedUpper)
    {
        // Arrange
        var ctx = TestParser.ParseOk($"real, {attributeText} :: x\n", p => p.declaration());
        var diagnostics = new DiagnosticBag();
        var builder = new AstBuilder("test.f90", diagnostics);

        // Act
        var ast = builder.Build<TypeDecl>(ctx);

        // Assert
        Assert.Empty(diagnostics);
        var dimension = Assert.IsType<DimensionAttr>(Assert.Single(ast.Attributes));
        var dim = Assert.Single(dimension.Dims);

        Assert.Equal(expectedLower, dim.Lower is IntLiteral lower ? lower.Value : null);
        Assert.Equal(expectedUpper, dim.Upper is IntLiteral upper ? upper.Value : null);
    }

    /// <summary>
    /// <c>integer :: a(10), b(1:5, 0:*)</c>
    /// </summary>
    [Fact]
    public void Build_EntityDimensions_ShouldPreserveEachDimension()
    {
        // Arrange
        var ctx = TestParser.ParseOk("integer :: a(10), b(1:5, 0:*)\n", p => p.declaration());
        var diagnostics = new DiagnosticBag();
        var builder = new AstBuilder("test.f90", diagnostics);

        // Act
        var ast = builder.Build<TypeDecl>(ctx);

        // Assert
        Assert.Empty(diagnostics);
        Assert.Equal(2, ast.Entities.Length);

        Assert.Equal("a", ast.Entities[0].Name);
        var aUpper = Assert.IsType<IntLiteral>(Assert.Single(ast.Entities[0].Dims).Upper);
        Assert.Equal(10L, aUpper.Value);

        Assert.Equal("b", ast.Entities[1].Name);
        Assert.Equal(2, ast.Entities[1].Dims.Length);
        Assert.Equal(1L, Assert.IsType<IntLiteral>(ast.Entities[1].Dims[0].Lower).Value);
        Assert.Equal(5L, Assert.IsType<IntLiteral>(ast.Entities[1].Dims[0].Upper).Value);
        Assert.Equal(0L, Assert.IsType<IntLiteral>(ast.Entities[1].Dims[1].Lower).Value);
        Assert.Null(ast.Entities[1].Dims[1].Upper);
    }
}
