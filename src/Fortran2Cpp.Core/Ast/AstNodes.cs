using System.Collections.Immutable;

namespace Fortran2Cpp.Core.Ast;

// =============================================================================
// UGOVOR (nacrt): AST
//
// Pravila:
//  - AST je verna slika SINTAKSE. Ne razrešava tipove, simbole ni dvosmislenosti;
//    to radi SemanticAnalyzer i upisuje u SemanticModel.
//  - Svi čvorovi su nepromenljivi i svaki ima SourceSpan.
//  - Sva imena su normalizovana na mala slova (Fortran ne razlikuje velika i mala slova).
//  - Ne zavisi ni od ANTLR-a ni od C++-a.
//  - SemanticModel čvorove prepoznaje po REFERENCI, a ne po vrednosti (vidi SemanticModel).
// =============================================================================

public abstract record Node(SourceSpan Span);

// -----------------------------------------------------------------------------
// Fajl i programske jedinice
// -----------------------------------------------------------------------------

/// <summary>Jedan izvorni fajl. U fazi 2 sadrži i module.</summary>
public sealed record CompilationUnit(
    string File,
    ImmutableArray<ProgramUnit> Units,
    SourceSpan Span) : Node(Span);

/// <summary>
/// Zajednički oblik za PROGRAM, SUBROUTINE i FUNCTION:
/// deklaracije (specification part), naredbe (execution part) i interne procedure (CONTAINS).
/// </summary>
public abstract record ProgramUnit(
    string Name,
    ImmutableArray<Decl> Decls,
    ImmutableArray<Stmt> Body,
    ImmutableArray<Procedure> Contains,
    SourceSpan Span) : Node(Span);

public sealed record MainProgram(
    string Name,
    ImmutableArray<Decl> Decls,
    ImmutableArray<Stmt> Body,
    ImmutableArray<Procedure> Contains,
    SourceSpan Span) : ProgramUnit(Name, Decls, Body, Contains, Span);

/// <summary>SUBROUTINE ili FUNCTION, eksterna ili interna (iz CONTAINS).</summary>
public abstract record Procedure(
    string Name,
    ImmutableArray<string> Params,
    ImmutableArray<Decl> Decls,
    ImmutableArray<Stmt> Body,
    ImmutableArray<Procedure> Contains,
    SourceSpan Span) : ProgramUnit(Name, Decls, Body, Contains, Span);

public sealed record Subroutine(
    string Name,
    ImmutableArray<string> Params,
    ImmutableArray<Decl> Decls,
    ImmutableArray<Stmt> Body,
    ImmutableArray<Procedure> Contains,
    SourceSpan Span) : Procedure(Name, Params, Decls, Body, Contains, Span);

/// <param name="Prefix">Tip ispred FUNCTION, npr. <c>real function f(x)</c>; null ako ga nema.</param>
/// <param name="ResultName">Ime iz <c>RESULT(r)</c>; null znači da je rezultat promenljiva sa imenom funkcije.</param>
public sealed record Function(
    string Name,
    ImmutableArray<string> Params,
    TypeSpec? Prefix,
    string? ResultName,
    ImmutableArray<Decl> Decls,
    ImmutableArray<Stmt> Body,
    ImmutableArray<Procedure> Contains,
    SourceSpan Span) : Procedure(Name, Params, Decls, Body, Contains, Span);

// -----------------------------------------------------------------------------
// Deklaracije
// -----------------------------------------------------------------------------

public abstract record Decl(SourceSpan Span) : Node(Span);

public sealed record ImplicitNone(SourceSpan Span) : Decl(Span);

/// <summary><c>integer, parameter :: n = 10, m = 2</c> ili <c>real a(5), b</c>.</summary>
public sealed record TypeDecl(
    TypeSpec Type,
    ImmutableArray<DeclAttribute> Attributes,
    ImmutableArray<EntityDecl> Entities,
    SourceSpan Span) : Decl(Span);

/// <summary>Stari oblik: <c>PARAMETER (n = 10, pi = 3.14)</c>.</summary>
public sealed record ParameterStmt(
    ImmutableArray<NamedConstant> Constants,
    SourceSpan Span) : Decl(Span);

public sealed record NamedConstant(string Name, Expr Value, SourceSpan Span) : Node(Span);

public enum BaseType
{
    Integer,
    Real,
    DoublePrecision,
    Logical,
    Character,
}

/// <param name="Kind">
/// <c>integer(kind=8)</c>, <c>real(8)</c>. Null ako nije naveden. Faza 1 ga ne podržava
/// (UnsupportedDetector prijavljuje), ali ostaje u AST-u da bi se prijavilo tačno mesto.
/// </param>
/// <param name="Length">Za CHARACTER: <c>character(len=10)</c>, <c>character*10</c>; null znači 1.</param>
public sealed record TypeSpec(
    BaseType Base,
    Expr? Kind,
    CharLength? Length,
    SourceSpan Span) : Node(Span);

/// <summary>Dužina stringa: izraz, ili <c>*</c> (assumed length, za dummy argumente).</summary>
public sealed record CharLength(Expr? Value, bool IsAssumed, SourceSpan Span) : Node(Span);

public abstract record DeclAttribute(SourceSpan Span) : Node(Span);
public sealed record ParameterAttr(SourceSpan Span) : DeclAttribute(Span);
public sealed record IntentAttr(Intent Intent, SourceSpan Span) : DeclAttribute(Span);
public sealed record DimensionAttr(ImmutableArray<DimSpec> Dims, SourceSpan Span) : DeclAttribute(Span);

public enum Intent
{
    In,
    Out,
    InOut,
}

/// <summary>Jedno ime u deklaraciji: <c>a(10)</c>, <c>s*20</c>, <c>n = 5</c>.</summary>
/// <param name="Dims">Dimenzije navedene uz ime; prazno ako ih nema (tada važe one iz DIMENSION, ako postoji).</param>
/// <param name="Length">Dužina navedena uz ime (<c>s*20</c>); ima prednost nad TypeSpec.Length.</param>
public sealed record EntityDecl(
    string Name,
    ImmutableArray<DimSpec> Dims,
    CharLength? Length,
    Expr? Init,
    SourceSpan Span) : Node(Span);

/// <summary>
/// Jedna dimenzija: <c>10</c> (Lower = null, znači 1), <c>0:9</c>, <c>n</c>.
/// Upper = null znači <c>*</c> ili <c>:</c>; faza 1 to ne podržava.
/// </summary>
public sealed record DimSpec(Expr? Lower, Expr? Upper, SourceSpan Span) : Node(Span);

// -----------------------------------------------------------------------------
// Naredbe
// -----------------------------------------------------------------------------

public abstract record Stmt(SourceSpan Span) : Node(Span);

/// <param name="Target">NameExpr ili NameOrCall (element niza).</param>
public sealed record Assignment(Expr Target, Expr Value, SourceSpan Span) : Stmt(Span);

public sealed record CallStmt(string Name, ImmutableArray<Expr> Args, SourceSpan Span) : Stmt(Span);

/// <summary>
/// IF / ELSE IF / ELSE. I jednolinijski <c>if (c) x = 1</c> je IfStmt sa jednom granom.
/// </summary>
/// <param name="Else">null ako nema ELSE grane.</param>
public sealed record IfStmt(
    ImmutableArray<IfBranch> Branches,
    ImmutableArray<Stmt>? Else,
    SourceSpan Span) : Stmt(Span);

public sealed record IfBranch(Expr Condition, ImmutableArray<Stmt> Body, SourceSpan Span) : Node(Span);

/// <summary><c>[ime:] DO var = start, end [, step]</c>.</summary>
/// <param name="ConstructName">Ime konstrukta za <c>EXIT ime</c> / <c>CYCLE ime</c>; null ako nije dato.</param>
public sealed record DoLoop(
    string? ConstructName,
    string Var,
    Expr Start,
    Expr End,
    Expr? Step,
    ImmutableArray<Stmt> Body,
    SourceSpan Span) : Stmt(Span);

/// <summary><c>DO WHILE (cond)</c>. <c>DO</c> bez kontrole je DoWhile sa Condition = null.</summary>
public sealed record DoWhile(
    string? ConstructName,
    Expr? Condition,
    ImmutableArray<Stmt> Body,
    SourceSpan Span) : Stmt(Span);

public sealed record ExitStmt(string? ConstructName, SourceSpan Span) : Stmt(Span);
public sealed record CycleStmt(string? ConstructName, SourceSpan Span) : Stmt(Span);

/// <summary>
/// <c>PRINT *, ...</c> i <c>WRITE(*,*) ...</c>. Oba su list-directed izlaz na stdout,
/// pa su isti čvor. Drugi unit ili format AstBuilder prijavljuje kao Unsupported.
/// </summary>
public sealed record PrintStmt(ImmutableArray<Expr> Items, SourceSpan Span) : Stmt(Span);

/// <summary><c>READ *, ...</c> i <c>READ(*,*) ...</c>. Ciljevi su NameExpr ili NameOrCall.</summary>
public sealed record ReadStmt(ImmutableArray<Expr> Targets, SourceSpan Span) : Stmt(Span);

public sealed record ReturnStmt(SourceSpan Span) : Stmt(Span);

/// <param name="Code">Celobrojni ili string kod iz <c>STOP 1</c>; null ako nije dat.</param>
public sealed record StopStmt(Expr? Code, SourceSpan Span) : Stmt(Span);

/// <summary>
/// Naredba koju parser prepozna, ali faza ne podržava (GOTO, COMMON...).
/// Čuva se u AST-u da bi UnsupportedDetector prijavio tačno mesto; CodeGen je nikad ne dobije
/// ako postoji Unsupported dijagnostika.
/// </summary>
public sealed record UnsupportedStmt(string Keyword, string Text, SourceSpan Span) : Stmt(Span);

// -----------------------------------------------------------------------------
// Izrazi
// -----------------------------------------------------------------------------

public abstract record Expr(SourceSpan Span) : Node(Span);

/// <param name="Text">Originalni zapis (npr. <c>10</c>), da se ne izgubi ništa pri ispisu.</param>
public sealed record IntLiteral(long Value, string Text, SourceSpan Span) : Expr(Span);

/// <summary>
/// Realni literal. Vrednost se NE parsira u double ovde, da se ne izgubi preciznost;
/// CodeGen od Text-a pravi C++ literal (<c>1.0</c> → <c>1.0f</c>, <c>1.0d0</c> → <c>1.0</c>).
/// </summary>
/// <param name="Text">Originalni zapis, npr. <c>1.5e3</c>, <c>2.0d0</c>, <c>.5</c>.</param>
/// <param name="IsDouble">true ako eksponent koristi <c>d</c>/<c>D</c>.</param>
public sealed record RealLiteral(string Text, bool IsDouble, SourceSpan Span) : Expr(Span);

public sealed record LogicalLiteral(bool Value, SourceSpan Span) : Expr(Span);

/// <param name="Value">Sadržaj bez navodnika, sa već razrešenim udvojenim navodnicima (<c>'it''s'</c> → <c>it's</c>).</param>
public sealed record StringLiteral(string Value, SourceSpan Span) : Expr(Span);

/// <summary>Samo ime, bez zagrada: <c>x</c>.</summary>
public sealed record NameExpr(string Name, SourceSpan Span) : Expr(Span);

/// <summary>
/// Ime sa zagradama: <c>a(i)</c>, <c>f(x)</c>, <c>f()</c>. Da li je element niza ili poziv
/// funkcije, razrešava SemanticAnalyzer (SemanticModel.GetNameOrCallKind).
/// </summary>
public sealed record NameOrCall(string Name, ImmutableArray<Expr> Args, SourceSpan Span) : Expr(Span);

public sealed record UnaryExpr(UnaryOp Op, Expr Operand, SourceSpan Span) : Expr(Span);

public sealed record BinaryExpr(BinaryOp Op, Expr Left, Expr Right, SourceSpan Span) : Expr(Span);

/// <summary>
/// Zagrade iz izvora se čuvaju: u Fortranu menjaju redosled izračunavanja
/// (bitno za realne brojeve), a CodeGen ih zadržava.
/// </summary>
public sealed record ParenExpr(Expr Inner, SourceSpan Span) : Expr(Span);

public enum UnaryOp
{
    Plus,
    Minus,
    Not,
}

public enum BinaryOp
{
    // aritmetički
    Add,
    Sub,
    Mul,
    Div,
    Pow,

    // string
    Concat,

    // relacioni (.eq. i == su isti operator)
    Eq,
    Ne,
    Lt,
    Le,
    Gt,
    Ge,

    // logički
    And,
    Or,
    Eqv,
    Neqv,
}
