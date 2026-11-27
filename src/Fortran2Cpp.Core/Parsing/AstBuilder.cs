using System.Collections.Immutable;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Fortran2Cpp.Core.Ast;
using Fortran2Cpp.Core.Grammar;
using Fortran2Cpp.Core.Diagnostics;

public sealed class AstBuilder : FortranBaseVisitor<Node>
{
    private readonly string _file;
    private readonly DiagnosticBag _diagnostics;
    public AstBuilder(string file, DiagnosticBag diagnostics)
    {
        _file = file;
        _diagnostics = diagnostics;
    }

    private SourceSpan Span(ParserRuleContext ctx)
    {
        var s = ctx.Start;
        var e = ctx.Stop ?? ctx.Start;
        return new SourceSpan(_file, s.Line, s.Column + 1, e.Line, e.Column + e.Text.Length);
    }

    public T Build<T>(IParseTree ctx) where T : Node => (T)Visit(ctx);
    public ImmutableArray<T> BuildAll<T>(IEnumerable<IParseTree> ctxs) where T : Node =>
        ctxs.Select(Build<T>).ToImmutableArray();

    public override Node VisitExprOnly(FortranParser.ExprOnlyContext ctx) =>
        Build<Expr>(ctx.expr());

    //Variable and literal nodes
    public override Node VisitVariable(FortranParser.VariableContext ctx) => 
        new NameExpr(ctx.ID().GetText().ToLowerInvariant(), Span(ctx));

    public override Node VisitIntLit(FortranParser.IntLitContext ctx)
    {
        var text = ctx.INT_LIT().GetText();
        var digits = text.Split('_')[0];
        return new IntLiteral(long.Parse(digits), text, Span(ctx));
    }

    public override Node VisitRealLit(FortranParser.RealLitContext ctx)
    {
        var text = ctx.REAL_LIT().GetText();
        var lower = text.ToLowerInvariant();
        var mantissa = lower.Split('_')[0];
        bool isDouble = mantissa.Contains('d') || lower.EndsWith("_8");
        return new RealLiteral(text, isDouble, Span(ctx));
    }

    public override Node VisitStringLit(FortranParser.StringLitContext ctx)
    {
        var raw = ctx.STRING_LIT().GetText();
        var quote = raw[0];
        var body = raw[1..^1].Replace(new string(quote, 2), quote.ToString());
        return new StringLiteral(body, Span(ctx));
    }

    public override Node VisitLogicalLit(FortranParser.LogicalLitContext ctx) => 
        new LogicalLiteral(ctx.LOGICAL_LIT().GetText().ToLowerInvariant() == ".true.", Span(ctx));

    //Unary operators and parentheses
    public override Node VisitParen(FortranParser.ParenContext ctx) =>
        new ParenExpr(Build<Expr>(ctx.expr()), Span(ctx));

    public override Node VisitUnary(FortranParser.UnaryContext ctx) =>
        new UnaryExpr(ctx.addOp().GetText() == "-" ? UnaryOp.Minus : UnaryOp.Plus, Build<Expr>(ctx.expr()), Span(ctx));

    public override Node VisitNot(FortranParser.NotContext ctx) =>
        new UnaryExpr(UnaryOp.Not, Build<Expr>(ctx.expr()), Span(ctx));


    //Binary operators
    private Node Bin(BinaryOp op, FortranParser.ExprContext l, FortranParser.ExprContext r, ParserRuleContext ctx) =>
        new BinaryExpr(op, Build<Expr>(l), Build<Expr>(r), Span(ctx));

    public override Node VisitPower(FortranParser.PowerContext ctx) =>
        Bin(BinaryOp.Pow, ctx.expr(0), ctx.expr(1), ctx);

    public override Node VisitMulDiv(FortranParser.MulDivContext ctx) =>
        Bin(ctx.mulOp().GetText() == "*" ? BinaryOp.Mul : BinaryOp.Div, ctx.expr(0), ctx.expr(1), ctx);

    public override Node VisitAddSub(FortranParser.AddSubContext ctx) =>
        Bin(ctx.addOp().GetText() == "+" ? BinaryOp.Add : BinaryOp.Sub, ctx.expr(0), ctx.expr(1), ctx);

    public override Node VisitConcat(FortranParser.ConcatContext ctx) =>
        Bin(BinaryOp.Concat, ctx.expr(0), ctx.expr(1), ctx);

    public override Node VisitRelational(FortranParser.RelationalContext ctx) =>
        Bin(ctx.relOp().GetText().ToLowerInvariant() switch
        {
            "==" or ".eq." => BinaryOp.Eq,
            "/=" or ".ne." => BinaryOp.Ne,
            "<" or ".lt." => BinaryOp.Lt,
            "<=" or ".le." => BinaryOp.Le,
            ">" or ".gt." => BinaryOp.Gt,
            ">=" or ".ge." => BinaryOp.Ge,
            var t => throw new InvalidOperationException($"Unknown relational operator '{t}'")
        }, ctx.expr(0), ctx.expr(1), ctx);

    public override Node VisitAnd(FortranParser.AndContext ctx) => Bin(BinaryOp.And, ctx.expr(0), ctx.expr(1), ctx);

    public override Node VisitOr(FortranParser.OrContext ctx) => Bin(BinaryOp.Or, ctx.expr(0), ctx.expr(1), ctx);

    public override Node VisitEqv(FortranParser.EqvContext ctx) =>
       Bin(ctx.eqvOp().GetText().ToLowerInvariant() == ".eqv." ? BinaryOp.Eqv : BinaryOp.Neqv,
           ctx.expr(0), ctx.expr(1), ctx);

    //Calls, indexing and array constructors
    public override Node VisitNameOrCall(FortranParser.NameOrCallContext ctx) =>
        new NameOrCall(
            ctx.callName().GetText().ToLowerInvariant(),
            ctx.subscript().Select(Subscript).ToImmutableArray(),
            Span(ctx));

    private Expr Subscript(FortranParser.SubscriptContext ctx)
    {
        if (!ctx.children.OfType<ITerminalNode>().Any())
            return Build<Expr>(ctx.expr(0));

        Expr? lower = null, upper = null, step = null;
        int colons = 0;
        foreach (var child in ctx.children)
        {
            if (child is ITerminalNode) { colons++; continue; }
            var e = Build<Expr>(child);
            if (colons == 0) lower = e;
            else if (colons == 1) upper = e;
            else step = e;
        }
        return new RangeExpr(lower, upper, step, Span(ctx));
    }

    public override Node VisitArrayConstructor(FortranParser.ArrayConstructorContext ctx) =>
        new ArrayConstructor(ctx.exprList().expr().Select(Build<Expr>).ToImmutableArray(), Span(ctx));

    public override Node VisitArrayConstructor2(FortranParser.ArrayConstructor2Context ctx) =>
        new ArrayConstructor(ctx.exprList().expr().Select(Build<Expr>).ToImmutableArray(), Span(ctx));

    //Declarations
    public override Node VisitImplicitStmt(FortranParser.ImplicitStmtContext ctx) =>
        new ImplicitNone(Span(ctx));

    public override Node VisitDeclaration(FortranParser.DeclarationContext ctx) =>
        new TypeDecl(
            Build<TypeSpec>(ctx.typeSpec()),
            BuildAll<DeclAttribute>(ctx.attr()),
            BuildAll<EntityDecl>(ctx.entityDecl()),
            Span(ctx));

    public override Node VisitTypeSpec(FortranParser.TypeSpecContext ctx)
    {
        var baseType =
            ctx.INTEGER() != null ? BaseType.Integer :
            ctx.REAL() != null ? BaseType.Real :
            ctx.DOUBLE_PRECISION() != null ? BaseType.DoublePrecision :
            ctx.LOGICAL() != null ? BaseType.Logical :
                                             BaseType.Character;

        Expr? kind = null;
        CharLength? length = null;
        var parameters = ctx.kindSelector()?.typeParam() ?? Array.Empty<FortranParser.TypeParamContext>();

        for (int i = 0; i < parameters.Length; i++)
        {
            var p = parameters[i];
            var name = p.ID()?.GetText().ToLowerInvariant();    // "kind", "len" ili null
            bool isStar = p.expr() == null;                      // character(len=*)
            Expr? value = isStar ? null : Build<Expr>(p.expr());

            if (baseType == BaseType.Character)
            {
                if (name == "len" || (name == null && i == 0))
                    length = new CharLength(value, isStar, Span(p));
                else if (name == "kind" || (name == null && i == 1))
                    kind = value;
            }
            else if (name == "kind" || name == null)
            {
                kind = value;
            }
        }

        return new TypeSpec(baseType, kind, length, Span(ctx));
    }

    public override Node VisitAttr(FortranParser.AttrContext ctx)
    {
        if (ctx.PARAMETER() != null)
            return new ParameterAttr(Span(ctx));

        if (ctx.INTENT() != null)
        {
            var text = string.Concat(ctx.ID().Select(id => id.GetText())).ToLowerInvariant();
            Intent intent;
            switch (text)
            {
                case "in":
                    intent = Intent.In;
                    break;
                case "out":
                    intent = Intent.Out;
                    break;
                case "inout":
                    intent = Intent.InOut;
                    break;
                default:
                    _diagnostics.Error(DiagnosticCodes.InvalidIntent,
                        $"Unknown intent '{text}' (expected in, out or inout)", Span(ctx));
                    intent = Intent.In;
                    break;
            }

            return new IntentAttr(intent, Span(ctx));
        }

        if (ctx.DIMENSION() != null)
            return new DimensionAttr(BuildAll<DimSpec>(ctx.dimSpec()), Span(ctx));

        return new GenericAttr(ctx.ID(0).GetText().ToLowerInvariant(), Span(ctx));
    }

    public override Node VisitEntityDecl(FortranParser.EntityDeclContext ctx) =>
        new EntityDecl(
            ctx.ID().GetText().ToLowerInvariant(),
            BuildAll<DimSpec>(ctx.dimSpec()),
            null,         //The character length spec is not supported in phase 1, so we always return null here.                          
            ctx.expr() == null ? null : Build<Expr>(ctx.expr()),
            Span(ctx));

    public override Node VisitDimSpec(FortranParser.DimSpecContext ctx)
    {
        var exprs = ctx.expr();
        bool hasColon = ctx.children.OfType<ITerminalNode>().Any(t => t.GetText() == ":");
        Expr? lower = null, upper = null;

        //No colon means that the dimension is explicit, ex. a(10)
        if (!hasColon)
        {
            if (exprs.Length == 1) upper = Build<Expr>(exprs[0]);
        }
        else
        {
            if (exprs.Length >= 1) lower = Build<Expr>(exprs[0]);
            if (exprs.Length == 2) upper = Build<Expr>(exprs[1]);
        }

        return new DimSpec(lower, upper, Span(ctx));
    }
}