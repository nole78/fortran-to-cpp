using System.Collections.Immutable;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Fortran2Cpp.Core.Ast;
using Fortran2Cpp.Core.Grammar;

public sealed class AstBuilder : FortranBaseVisitor<Node>
{
    private readonly string _file;
    public AstBuilder(string file) => _file = file;

    private SourceSpan Span(ParserRuleContext ctx)
    {
        var s = ctx.Start;
        var e = ctx.Stop ?? ctx.Start;
        return new SourceSpan(_file, s.Line, s.Column + 1, e.Line, e.Column + e.Text.Length);
    }

    public Expr BuildExpr(IParseTree ctx) => (Expr)Visit(ctx);

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
        new ParenExpr(BuildExpr(ctx.expr()), Span(ctx));

    public override Node VisitUnary(FortranParser.UnaryContext ctx) =>
        new UnaryExpr(ctx.addOp().GetText() == "-" ? UnaryOp.Minus : UnaryOp.Plus, BuildExpr(ctx.expr()), Span(ctx));

    public override Node VisitNot(FortranParser.NotContext ctx) =>
        new UnaryExpr(UnaryOp.Not, BuildExpr(ctx.expr()), Span(ctx));


    //Binary operators
    private Node Bin(BinaryOp op, FortranParser.ExprContext l, FortranParser.ExprContext r, ParserRuleContext ctx) =>
        new BinaryExpr(op, BuildExpr(l), BuildExpr(r), Span(ctx));

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
            return BuildExpr(ctx.expr(0));

        Expr? lower = null, upper = null, step = null;
        int colons = 0;
        foreach (var child in ctx.children)
        {
            if (child is ITerminalNode) { colons++; continue; }
            var e = BuildExpr(child);
            if (colons == 0) lower = e;
            else if (colons == 1) upper = e;
            else step = e;
        }
        return new RangeExpr(lower, upper, step, Span(ctx));
    }

    public override Node VisitArrayConstructor(FortranParser.ArrayConstructorContext ctx) =>
        new ArrayConstructor(ctx.exprList().expr().Select(BuildExpr).ToImmutableArray(), Span(ctx));

    public override Node VisitArrayConstructor2(FortranParser.ArrayConstructor2Context ctx) =>
        new ArrayConstructor(ctx.exprList().expr().Select(BuildExpr).ToImmutableArray(), Span(ctx));
}