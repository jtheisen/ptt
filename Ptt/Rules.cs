using System.Diagnostics.CodeAnalysis;

namespace Ptt;

public record Rule(String Name, Symbol[] AllQuantifiedSymbols, params Relation[] Summands)
{
    public static Rule Implication(String name, Symbol[] allQuantifiedSymbols, Relation corollary, params Relation[] conditions)
        => new Rule(name, allQuantifiedSymbols, corollary.Singleton().Concat(conditions.Select(c => c with { IsNegated = !c.IsNegated })).ToArray());

    public override String ToString() => String.Join(" ∨ ", from s in Summands select s);
}



public class RuleSet : AbstractRuleSet
{
    List<Rule> rules;

    protected override IEnumerable<Rule> GetRules() => rules;

    public static RuleSet Build(Action<RuleSet> builder)
    {
        var rs = new RuleSet();
        builder(rs);
        return rs;
    }

    public RuleSet()
    {
        rules = new List<Rule>();
    }

    public RuleSet(IEnumerable<Rule> rules)
    {
        this.rules = rules.ToList();
    }

    public void AddRule(Rule rule) => rules.Add(rule);

    public void AddImplication(String name, Symbol[] allQuantifiedSymbols, Relation corollary, params Relation[] conditions)
        => rules.Add(Rule.Implication(name, allQuantifiedSymbols, corollary, conditions));
}

public abstract class AbstractRuleSet
{
    public AbstractRuleSet Reduce(Relation assumption)
        => new RuleSet(GetRules().Where(r => r.Summands.Length > 1).SelectMany(r => r.GetReducedRules(assumption)));

    public IEnumerable<Relation> GetSuggestionsAsRelations(Expression expression)
        => GetSuggestions(expression).Select(p => new Relation(p.Symbol, expression, p.Expression, p.IsReversed, p.IsNegated));

    public IEnumerable<ChainPart> GetSuggestions(Expression expression)
    {
        foreach (var rule in GetSimpleRules())
        {
            if (rule.TryApplyRule(expression, out var part1, true))
            {
                yield return part1;
            }

            if (rule.TryApplyRule(expression, out var part2, false))
            {
                yield return part2;
            }
        }
    }

    protected virtual IEnumerable<Rule> GetSimpleRules() => GetRules().Where(r => r.IsSimple());

    protected abstract IEnumerable<Rule> GetRules();

    public override String ToString()
    {
        return String.Join("\n", GetRules());
    }
}

public class RuleSetBuilder
{
    List<Rule> rules = new List<Rule>();

    public RuleSetBuilder AddImplication(String name, Symbol[] allQuantifiedSymbols, Relation corollary, params Relation[] conditions)
        => Modify(() => rules.Add(Rule.Implication(name, allQuantifiedSymbols, corollary, conditions)));

    RuleSetBuilder Modify(Action action) { action(); return this; }

    public RuleSet Build() => new RuleSet(rules);
}

public static partial class Extensions
{
    public static AbstractRuleSet ToRuleSet(this IEnumerable<Rule> rules) => new RuleSet(rules);

    public static IEnumerable<Rule> GetReducedRules(this IEnumerable<Rule> rules, Relation assumption)
        => rules.SelectMany(r => r.GetReducedRules(assumption));

    public static IEnumerable<Rule> GetReducedRules(this Rule rule, Relation assumption)
    {
        foreach (var summand in rule.Summands)
        {
            var substitutions = rule.AllQuantifiedSymbols.GetSubstitutionDictionary();

            if (!summand.TryUnify(assumption, true, out var result, ref substitutions)) continue;

            yield return new Rule(
                rule.Name,
                rule.AllQuantifiedSymbols.WithoutSubstitutions(substitutions).ToArray(),
                (from s in rule.Summands where !Object.ReferenceEquals(s, summand) select s.Substitute(substitutions)).ToArray()
            );
        }
    }

    public static Boolean IsSimple(this Rule rule)
        => rule.Summands.Length == 1;

    public static ChainPart? ApplyRule(this Rule rule, Expression expression, Boolean forward)
        => rule.TryApplyRule(expression, out var result, forward) ? result : null;

    public static Boolean TryApplyRule(this Rule rule, Expression expression, [NotNullWhen(true)] out ChainPart? result, Boolean forward = true)
    {
        result = null;

        // FIXME
        var (symbol, lhs, rhs, isReversed, isNegated) = rule.Summands.Single();

        var (e1, e2) = forward ? (lhs, rhs) : (rhs, lhs);

        var substitutions = rule.AllQuantifiedSymbols.GetSubstitutionDictionary();

        var unified = Extensions.Unify(e1, expression, ref substitutions);

        if (unified is null) return false;

        result = new ChainPart(symbol, e2.Substitute(substitutions));

        return true;
    }
}