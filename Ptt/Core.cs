using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace Ptt;

public record Expression
{
    static readonly Symbol SymbolProduct
        = new Symbol("*");

    static readonly Symbol SymbolSum
        = new Symbol("+");

    static readonly Symbol SymbolMinus
        = new Symbol("-");

    static readonly Symbol SymbolDivision
        = new Symbol("/");

    public static Expression operator +(Expression lhs, Expression rhs) => new BinaryExpression(SymbolSum, lhs, rhs);
    public static Expression operator -(Expression lhs, Expression rhs) => new BinaryExpression(SymbolMinus, lhs, rhs);
    public static Expression operator *(Expression lhs, Expression rhs) => new BinaryExpression(SymbolProduct, lhs, rhs);
    public static Expression operator /(Expression lhs, Expression rhs) => new BinaryExpression(SymbolDivision, lhs, rhs);

    public static Expression operator -(Expression op) => new UnaryExpression(SymbolMinus, op);

    public Object? Ui { get; set; }

    public virtual IEnumerable<Expression> Children => Enumerable.Empty<Expression>();

    public virtual Expression Map(Func<Expression, Expression> mapping) => throw new NotImplementedException();
}

public record Symbol(String Name)
{
    public override String ToString() => Name;
}

public record AtomExpression(Symbol Symbol) : Expression
{
    public static Symbol operator ~(AtomExpression op) => op.Symbol;

    public override Expression Map(Func<Expression, Expression> mapping) => this;

    public override String ToString() => $"{Symbol}";
}

public record UnaryExpression(Symbol Symbol, Expression Op) : Expression
{
    public override Expression Map(Func<Expression, Expression> mapping) => new UnaryExpression(Symbol, mapping(Op));

    public override IEnumerable<Expression> Children
    {
        get
        {
            yield return Op;
        }
    }

    public override String ToString() => $"{Symbol}({Op})";
}

public record BinaryExpression(Symbol Symbol, Expression Lhs, Expression Rhs) : Expression
{
    public override Expression Map(Func<Expression, Expression> mapping)
        => new BinaryExpression(Symbol, mapping(Lhs), mapping(Rhs));

    public override IEnumerable<Expression> Children
    {
        get
        {
            yield return Lhs;
            yield return Rhs;
        }
    }

    public override String ToString() => $"({Lhs} {Symbol} {Rhs})";
}

public record Relation(Symbol Symbol, Expression Lhs, Expression Rhs);

public record Rule(String Name, Symbol[] AllQuantifiedSymbols, Symbol Symbol, Expression Lhs, Expression Rhs);

public record ChainPart(Symbol Symbol, Expression Expression);

public static class Extensions
{
    public static Expression Substitute(this Expression expression, IDictionary<Symbol, Expression?> substitutions)
    {
        if (expression is AtomExpression a && substitutions.TryGetValue(a.Symbol, out var substitution) && substitution is not null)
        {
            return substitution;
        }
        else
        {
            return expression.Map(e => e.Substitute(substitutions));
        }
    }

    public static ChainPart? ApplyRule(this Rule rule, Expression expression)
    {
        var substitutions = rule.AllQuantifiedSymbols.ToDictionary(s => s, s => (Expression?)null);

        var unified = Extensions.Unify(substitutions, ImmutableDictionary.Create<Symbol, Symbol>(), rule.Lhs, expression);

        if (unified is null) return null;

        return new ChainPart(rule.Symbol, rule.Rhs.Substitute(substitutions));
    }

    public static Expression? Unify(IDictionary<Symbol, Expression?> subsitutions, IImmutableDictionary<Symbol, Symbol> symbolMapping, Expression template, Expression expression)
    {
        if (template is AtomExpression at)
        {
            if (expression is AtomExpression nt)
            {
                if (at.Symbol == nt.Symbol)
                {
                    return nt;
                }
            }

            if (subsitutions.TryGetValue(at.Symbol, out var subsitutation))
            {
                if (subsitutation is not null)
                {
                    return null;
                }
                else
                {
                    subsitutions[at.Symbol] = expression;

                    return expression;
                }
            }

            return null;
        }
        else if (template is BinaryExpression tb)
        {
            if (expression is not BinaryExpression eb || tb.Symbol != eb.Symbol) return null;

            var l = Unify(subsitutions, symbolMapping, tb.Lhs, eb.Lhs);
            var r = Unify(subsitutions, symbolMapping, tb.Rhs, eb.Rhs);

            if (l is not null && r is not null)
            {
                return new BinaryExpression(tb.Symbol, l, r);
            }
            else
            {
                return null;
            }
        }
        else if (template is UnaryExpression tu)
        {
            if (expression is not UnaryExpression eu || tu.Symbol != eu.Symbol) return null;

            var o = Unify(subsitutions, symbolMapping, tu.Op, eu.Op);

            if (o is not null)
            {
                return new UnaryExpression(tu.Symbol, o);
            }
            else
            {
                return null;
            }
        }
        else
        {
            throw new Exception($"Unkown not type {expression.GetType()}");
        }
    }

    public static IEnumerable<Expression> Unify(this IImmutableDictionary<Symbol, Expression> index, Expression node) => node switch
    {

        _ => Enumerable.Empty<Expression>()
    };
}