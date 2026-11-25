using System.Collections.Immutable;
using Fortran2Cpp.Core.Ast;

namespace Fortran2Cpp.Core.Semantics;

// =============================================================================
// UGOVOR (nacrt): SemanticModel
//
// SemanticAnalyzer (Anja) ga pravi preko SemanticModelBuilder-a, a CodeGen (David)
// ga samo čita. CodeGen nikad sam ne zaključuje tipove ni simbole: sve što mu treba
// pita ovde.
//
// Čvorovi se prepoznaju po REFERENCI (ReferenceEqualityComparer). AST record-i imaju
// vrednosnu jednakost, pa bi dva ista izraza `i` u istoj liniji inače bila isti ključ.
// =============================================================================

// -----------------------------------------------------------------------------
// Tipovi
// -----------------------------------------------------------------------------

/// <summary>Jedna dimenzija niza. Lower je uvek popunjen (podrazumevano IntLiteral 1).</summary>
/// <remarks>
/// Granice su izrazi, jer dummy argument može biti <c>real :: a(n)</c>.
/// Za konstantne granice SemanticModel.GetConstantValue vraća vrednost.
/// </remarks>
public sealed record ArrayDim(Expr Lower, Expr Upper);

/// <param name="Dims">Prazno za skalar. Faza 1: rang 1 ili 2.</param>
/// <param name="CharLength">
/// Samo za Character: dužina; null znači assumed length (<c>character(len=*)</c>).
/// Za ostale tipove se ignoriše.
/// </param>
public sealed record FortranType(
    BaseType Base,
    ImmutableArray<ArrayDim> Dims,
    long? CharLength = null)
{
    public int Rank => Dims.IsDefault ? 0 : Dims.Length;
    public bool IsArray => Rank > 0;
    public bool IsNumeric => Base is BaseType.Integer or BaseType.Real or BaseType.DoublePrecision;

    /// <summary>Isti tip bez dimenzija, npr. tip elementa niza.</summary>
    public FortranType ElementType => IsArray ? this with { Dims = [] } : this;

    public static readonly FortranType Integer = new(BaseType.Integer, []);
    public static readonly FortranType Real = new(BaseType.Real, []);
    public static readonly FortranType Double = new(BaseType.DoublePrecision, []);
    public static readonly FortranType Logical = new(BaseType.Logical, []);
    public static FortranType Character(long? length) => new(BaseType.Character, [], length);

    public FortranType WithDims(params ArrayDim[] dims) => this with { Dims = [.. dims] };

    // Vrednosna jednakost za ImmutableArray poredi reference, pa ovde poredimo ručno
    // samo bazni tip, dužinu i rang (granice su izrazi i ne porede se).
    public bool Equals(FortranType? other) =>
        other is not null && Base == other.Base && Rank == other.Rank && CharLength == other.CharLength;

    public override int GetHashCode() => HashCode.Combine(Base, Rank, CharLength);
}

// -----------------------------------------------------------------------------
// Simboli
// -----------------------------------------------------------------------------

/// <summary>
/// Simboli su klase (ne record-i), jer imaju cikluse (promenljiva → jedinica → promenljive)
/// i porede se po referenci. Prave se isključivo preko SemanticModelBuilder-a.
/// </summary>
public abstract class Symbol
{
    private protected Symbol(string name, SourceSpan declaredAt)
    {
        Name = name;
        DeclaredAt = declaredAt;
    }

    /// <summary>Ime malim slovima, kao u AST-u.</summary>
    public string Name { get; }

    public SourceSpan DeclaredAt { get; }

    public override string ToString() => $"{GetType().Name}({Name})";
}

public enum VariableKind
{
    Local,

    /// <summary>Dummy argument procedure. Prosleđuje se po referenci.</summary>
    Dummy,

    /// <summary>Promenljiva rezultata funkcije (ime funkcije ili ime iz RESULT).</summary>
    FunctionResult,

    /// <summary>PARAMETER konstanta. Vrednost je u ConstantValue.</summary>
    NamedConstant,
}

public sealed class VariableSymbol : Symbol
{
    internal VariableSymbol(
        string name,
        FortranType type,
        VariableKind kind,
        UnitSymbol owner,
        SourceSpan declaredAt,
        Intent? intent,
        bool isImplicitlyTyped,
        Expr? initializer,
        object? constantValue)
        : base(name, declaredAt)
    {
        Type = type;
        Kind = kind;
        Owner = owner;
        Intent = intent;
        IsImplicitlyTyped = isImplicitlyTyped;
        Initializer = initializer;
        ConstantValue = constantValue;
    }

    public FortranType Type { get; }
    public VariableKind Kind { get; }

    /// <summary>Jedinica u kojoj je deklarisana. Ako je različita od trenutne, to je host association.</summary>
    public UnitSymbol Owner { get; }

    /// <summary>Samo za Dummy; null znači da INTENT nije naveden.</summary>
    public Intent? Intent { get; }

    /// <summary>Nije deklarisana, tip je dobila po prvom slovu (I–N → INTEGER, ostalo REAL).</summary>
    public bool IsImplicitlyTyped { get; }

    /// <summary>Izraz iz deklaracije (<c>integer :: x = 0</c>) ili vrednost PARAMETER-a.</summary>
    public Expr? Initializer { get; }

    /// <summary>
    /// Za NamedConstant: izračunata vrednost (long, double, bool ili string).
    /// REAL i DOUBLE PRECISION su oba double; tip se gleda iz Type.
    /// </summary>
    public object? ConstantValue { get; }

    /// <summary>
    /// Fortran: lokalna promenljiva sa inicijalizacijom u deklaraciji implicitno ima SAVE,
    /// tj. zadržava vrednost između poziva. U C++-u to mora biti <c>static</c>.
    /// </summary>
    public bool IsSaved =>
        Kind == VariableKind.Local && Initializer is not null && Owner.Kind != UnitKind.Program;
}

public enum UnitKind
{
    Program,
    Subroutine,
    Function,
    // Module dolazi u fazi 2
}

public sealed class UnitSymbol : Symbol
{
    private readonly Dictionary<string, VariableSymbol> _variables = new(StringComparer.Ordinal);
    private readonly List<VariableSymbol> _orderedVariables = [];
    private readonly List<UnitSymbol> _internal = [];

    internal UnitSymbol(ProgramUnit node, UnitKind kind, UnitSymbol? host)
        : base(node.Name, node.Span)
    {
        Node = node;
        Kind = kind;
        Host = host;
    }

    public ProgramUnit Node { get; }
    public UnitKind Kind { get; }

    /// <summary>Jedinica koja sadrži ovu u CONTAINS; null za eksternu proceduru i program.</summary>
    public UnitSymbol? Host { get; }

    /// <summary>Interne procedure iz CONTAINS, redom kao u izvoru.</summary>
    public IReadOnlyList<UnitSymbol> Internal => _internal;

    /// <summary>
    /// SVE promenljive ove jedinice redom deklarisanja, uključujući dummy argumente,
    /// rezultat funkcije, konstante i implicitno tipizovane promenljive (koje nemaju Decl
    /// u AST-u, pa CodeGen deklaracije pravi odavde, a ne iz AST-a).
    /// </summary>
    public IReadOnlyList<VariableSymbol> Variables => _orderedVariables;

    /// <summary>Dummy argumenti redom iz zaglavlja procedure.</summary>
    public ImmutableArray<VariableSymbol> Params =>
        Node is Procedure p ? [.. p.Params.Select(n => _variables[n])] : [];

    /// <summary>Promenljiva rezultata; null ako jedinica nije funkcija.</summary>
    public VariableSymbol? Result =>
        Node is Function f ? _variables.GetValueOrDefault(f.ResultName ?? f.Name) : null;

    /// <summary>Samo promenljive ove jedinice; ne gleda u host.</summary>
    public VariableSymbol? Lookup(string name) => _variables.GetValueOrDefault(name);

    internal void AddVariable(VariableSymbol v)
    {
        if (!_variables.TryAdd(v.Name, v))
            throw new InvalidOperationException($"'{v.Name}' je već deklarisano u '{Name}'.");
        _orderedVariables.Add(v);
    }

    internal void AddInternal(UnitSymbol u) => _internal.Add(u);
}

// -----------------------------------------------------------------------------
// Model
// -----------------------------------------------------------------------------

public enum NameOrCallKind
{
    ArrayElement,
    FunctionCall,
    // IntrinsicCall dolazi u fazi 3
}

public sealed class SemanticModel
{
    private readonly Dictionary<Node, Symbol> _symbols;
    private readonly Dictionary<Expr, FortranType> _types;
    private readonly Dictionary<NameOrCall, NameOrCallKind> _kinds;
    private readonly Dictionary<Expr, object> _constants;

    internal SemanticModel(
        ImmutableArray<UnitSymbol> units,
        Dictionary<Node, Symbol> symbols,
        Dictionary<Expr, FortranType> types,
        Dictionary<NameOrCall, NameOrCallKind> kinds,
        Dictionary<Expr, object> constants)
    {
        Units = units;
        _symbols = symbols;
        _types = types;
        _kinds = kinds;
        _constants = constants;
    }

    /// <summary>Jedinice najvišeg nivoa (program i eksterne procedure), redom iz izvora.</summary>
    public ImmutableArray<UnitSymbol> Units { get; }

    /// <summary>
    /// Simbol na koji se čvor odnosi. Vezani su:
    /// NameExpr, NameOrCall, CallStmt, DoLoop (promenljiva petlje), EntityDecl,
    /// NamedConstant i ProgramUnit.
    /// </summary>
    public Symbol? GetSymbol(Node node) => _symbols.GetValueOrDefault(node);

    public VariableSymbol GetVariable(Node node) =>
        GetSymbol(node) as VariableSymbol
        ?? throw Missing(node, "promenljiva");

    public UnitSymbol GetUnit(Node node) =>
        GetSymbol(node) as UnitSymbol
        ?? throw Missing(node, "jedinica");

    /// <summary>Tip SVAKOG izraza, uključujući literale i međurezultate.</summary>
    public FortranType GetType(Expr expr) =>
        _types.TryGetValue(expr, out var t) ? t : throw Missing(expr, "tip");

    public NameOrCallKind GetNameOrCallKind(NameOrCall node) =>
        _kinds.TryGetValue(node, out var k) ? k : throw Missing(node, "vrsta (niz ili poziv)");

    /// <summary>Vrednost konstantnog izraza (long, double, bool ili string); null ako nije konstantan.</summary>
    public object? GetConstantValue(Expr expr) => _constants.GetValueOrDefault(expr);

    // Nedostajući podatak je bug u SemanticAnalyzer-u, ne greška u ulazu, pa je izuzetak opravdan.
    private static InvalidOperationException Missing(Node node, string what) =>
        new($"SemanticModel nema podatak '{what}' za {node.GetType().Name} na {node.Span}.");
}

/// <summary>
/// Jedini način da se napravi SemanticModel. Koristi ga SemanticAnalyzer, a CodeGen
/// testovi ga koriste da ručno naprave model bez parsera.
/// </summary>
public sealed class SemanticModelBuilder
{
    private readonly List<UnitSymbol> _units = [];
    private readonly Dictionary<Node, Symbol> _symbols = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<Expr, FortranType> _types = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<NameOrCall, NameOrCallKind> _kinds = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<Expr, object> _constants = new(ReferenceEqualityComparer.Instance);

    /// <summary>Deklariše jedinicu i veže je za njen AST čvor.</summary>
    public UnitSymbol DeclareUnit(ProgramUnit node, UnitSymbol? host = null)
    {
        var kind = node switch
        {
            MainProgram => UnitKind.Program,
            Subroutine => UnitKind.Subroutine,
            Function => UnitKind.Function,
            _ => throw new ArgumentException($"Nepoznata jedinica: {node.GetType().Name}"),
        };

        var unit = new UnitSymbol(node, kind, host);
        if (host is null) _units.Add(unit);
        else host.AddInternal(unit);

        _symbols[node] = unit;
        return unit;
    }

    public VariableSymbol DeclareVariable(
        UnitSymbol owner,
        string name,
        FortranType type,
        VariableKind kind = VariableKind.Local,
        Intent? intent = null,
        bool isImplicitlyTyped = false,
        Expr? initializer = null,
        object? constantValue = null,
        SourceSpan? declaredAt = null)
    {
        var v = new VariableSymbol(
            name, type, kind, owner, declaredAt ?? SourceSpan.None,
            intent, isImplicitlyTyped, initializer, constantValue);
        owner.AddVariable(v);
        return v;
    }

    public SemanticModelBuilder Bind(Node node, Symbol symbol)
    {
        _symbols[node] = symbol;
        return this;
    }

    public SemanticModelBuilder SetType(Expr expr, FortranType type)
    {
        _types[expr] = type;
        return this;
    }

    public SemanticModelBuilder SetKind(NameOrCall node, NameOrCallKind kind)
    {
        _kinds[node] = kind;
        return this;
    }

    public SemanticModelBuilder SetConstant(Expr expr, object value)
    {
        _constants[expr] = value;
        return this;
    }

    public SemanticModel Build() =>
        new([.. _units], new(_symbols, ReferenceEqualityComparer.Instance),
            new(_types, ReferenceEqualityComparer.Instance),
            new(_kinds, ReferenceEqualityComparer.Instance),
            new(_constants, ReferenceEqualityComparer.Instance));
}
