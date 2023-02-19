using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using static Ptt.FieldSymbols;
using static Ptt.RelationSymbols;

namespace Ptt;

public record Expression
{
    public Object? Ui { get; set; }

    public virtual IEnumerable<Expression> Children => Enumerable.Empty<Expression>();

    public virtual Expression Map(Func<Expression, Expression> mapping) => throw new NotImplementedException();

    public virtual Expression MakeClone() => throw new NotImplementedException();
}

public class DslExpression
{
    public Expression Expression { get; protected init; } = null!;

    public static implicit operator DslExpression(Expression expression) => new DslExpression { Expression = expression };
    public static implicit operator Expression(DslExpression expression) => expression.Expression;

    public static DslExpression operator +(DslExpression lhs, Expression rhs) => new BinaryExpression(SymFieldSum, lhs, rhs);
    public static DslExpression operator -(DslExpression lhs, Expression rhs) => new BinaryExpression(SymFieldMinus, lhs, rhs);
    public static DslExpression operator *(DslExpression lhs, Expression rhs) => new BinaryExpression(SymFieldProduct, lhs, rhs);
    public static DslExpression operator /(DslExpression lhs, Expression rhs) => new BinaryExpression(SymFieldDivision, lhs, rhs);

    public static DslExpression operator -(DslExpression op) => new UnaryExpression(SymFieldMinus, op);

    public static Relation operator ==(DslExpression lhs, Expression rhs) => new Relation(SymEq, lhs, rhs);
    public static Relation operator !=(DslExpression lhs, Expression rhs) => new Relation(SymEq, lhs, rhs, IsNegated: true);

    public static Relation operator <(DslExpression lhs, Expression rhs) => new Relation(SymLt, lhs, rhs);
    public static Relation operator >(DslExpression lhs, Expression rhs) => new Relation(SymLt, lhs, rhs, IsReversed: true);

    public static Relation operator <=(DslExpression lhs, Expression rhs) => new Relation(SymLt, lhs, rhs, IsReversed: true, IsNegated: true);
    public static Relation operator >=(DslExpression lhs, Expression rhs) => new Relation(SymLt, lhs, rhs, IsReversed: false, IsNegated: true);

    public override Boolean Equals(Object? obj) => throw new NotImplementedException();
    public override Int32 GetHashCode() => throw new NotImplementedException();
}

public class DslAtomExpression : DslExpression
{
    public static Symbol operator ~(DslAtomExpression self) => ((AtomExpression)self.Expression).Symbol;

    public DslAtomExpression(AtomExpression expression)
    {
        Expression = expression;
    }
}

public record Symbol(String Name, String? Backward = null, String? ForwardNegated = null, String? BackwardNegated = null)
{
    public override String ToString() => Name;

    public String ToString(Boolean reversed, Boolean negated)
    {
        return negated
            ? (reversed ? BackwardNegated ?? ForwardNegated ?? "n/a" : ForwardNegated ?? "n/a")
            : (reversed ? Backward ?? Name : Name);
    }
}


public record AtomExpression(Symbol Symbol) : Expression
{
    public override Expression Map(Func<Expression, Expression> mapping) => this;

    public override String ToString() => $"{Symbol}";

    public override Expression MakeClone() => new AtomExpression(Symbol);
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

    public override Expression MakeClone() => new UnaryExpression(Symbol, Op.MakeClone());
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

    public override Expression MakeClone() => new BinaryExpression(Symbol, Lhs.MakeClone(), Rhs.MakeClone());
}

public record Relation(Symbol Symbol, Expression Lhs, Expression Rhs, Boolean IsReversed = false, Boolean IsNegated = false)
{
    public static Relation operator !(Relation r) => r with { IsNegated = !r.IsNegated };

    public override String ToString() => $"{Lhs} {Symbol.ToString(IsReversed, IsNegated)} {Rhs}";
}

public record ChainPart(Symbol Symbol, Expression Expression, Boolean IsReversed = false, Boolean IsNegated = false)
{
    public override String ToString() => $"{Symbol.ToString(IsReversed, IsNegated)} {Expression}";
}

public static partial class Extensions
{
    public static String Format(this IEnumerable<ChainPart> parts)
        => String.Join("\n", parts);

    public static Relation Substitute(this Relation relation, IImmutableDictionary<Symbol, Expression?> substitutions)
        => relation with { Lhs = relation.Lhs.Substitute(substitutions), Rhs = relation.Rhs.Substitute(substitutions) };

    public static Expression Substitute(this Expression expression, IImmutableDictionary<Symbol, Expression?> substitutions)
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

    public static Relation ToRelation(this ChainPart chainPart, Expression lhs)
    {
        var (symbol, rhs, isReversed, isNegated) = chainPart;

        return new Relation(symbol, lhs, rhs, isReversed, isNegated);
    }

    public static Expression Host(this Expression expression, Action<Expression> visitor)
    {
        visitor(expression);

        foreach (var child in expression.Children)
        {
            child.Host(visitor);
        }

        return expression;
    }

    public static Expression AssertNonUi(this Expression expression)
        => expression.Host(e => { if (e.Ui is not null) throw new Exception("Expression is used in UI"); });

    public static ChainPart ToChainPart(this Relation relation)
    {
        var (symbol, lhs, rhs, isReversed, isNegated) = relation;

        if (lhs is not AtomExpression ae || ae.Symbol.Name != "__") throw new Exception($"Expected the placeholder symbol on the left side");

        return new ChainPart(symbol, rhs, isReversed, isNegated);
    }

    public static IImmutableDictionary<Symbol, Expression?> GetSubstitutionDictionary(this IEnumerable<Symbol> symbols)
        => symbols.ToImmutableDictionary(s => s, s => (Expression?)null);

    public static IEnumerable<Symbol> WithoutSubstitutions(this IEnumerable<Symbol> symbols, IImmutableDictionary<Symbol, Expression?> substitutions)
        => from s in symbols where substitutions[s] is null select s;

    public static Boolean TryUnify(this Relation template, Relation source, Boolean negated, [NotNullWhen(true)] out Relation? result, ref IImmutableDictionary<Symbol, Expression?> substitutions)
    {
        result = null;

        if (template.Symbol != source.Symbol) return false;

        var (symbol, tl, tr, tIsReversed, tIsNegated) = template;
        var (_, sl, sr, sIsReversed, sIsNegated) = source;

        if ((tIsNegated != sIsNegated) != negated) return false;

        if (tIsReversed != sIsReversed) return false;

        if (!tl.TryUnify(sl, out var lhs, ref substitutions)) return false;
        if (!tr.TryUnify(sr, out var rhs, ref substitutions)) return false;

        result = new Relation(symbol, lhs, rhs, tIsReversed, tIsNegated);

        return true;
    }

    public static Boolean TryUnify(this Expression template, Expression expression, [NotNullWhen(true)] out Expression? result, ref IImmutableDictionary<Symbol, Expression?> substitutions, IImmutableDictionary<Symbol, Symbol> symbolMapping = null)
    {
        result = Unify(template, expression, ref substitutions, symbolMapping);

        return result is not null;
    }

    public static Expression? Unify(Expression template, Expression expression, ref IImmutableDictionary<Symbol, Expression?> substitutions, IImmutableDictionary<Symbol, Symbol> symbolMapping = null)
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

            if (substitutions.TryGetValue(at.Symbol, out var subsitutation))
            {
                if (subsitutation is not null)
                {
                    return null;
                }
                else
                {
                    substitutions = substitutions.SetItem(at.Symbol, expression);

                    return expression;
                }
            }

            return null;
        }
        else if (template is BinaryExpression tb)
        {
            if (expression is not BinaryExpression eb || tb.Symbol != eb.Symbol) return null;

            var l = Unify(tb.Lhs, eb.Lhs, ref substitutions, symbolMapping);
            var r = Unify(tb.Rhs, eb.Rhs, ref substitutions, symbolMapping);

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

            var o = Unify(tu.Op, eu.Op, ref substitutions, symbolMapping);

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
}