namespace Ptt;

public class ReasoningChain
{
    public Expression Beginning { get; init; } = null!;

    public List<ChainPart> Parts = new List<ChainPart>();

    public void Add(Symbol symbol, Expression expression)
    {
        Parts.Add(new ChainPart(symbol, expression));
    }
}

public class RuleSet
{
    List<Rule> rules = new List<Rule>();

    public IEnumerable<ChainPart> GetSuggestions(Expression expression)
    {
        foreach (var rule in rules)
        {
            var chainPart = rule.ApplyRule(expression);

            if (chainPart is null) continue;

            yield return chainPart;
        }
    }

}

public class UiChainPart
{
    public Symbol Symbol { get; init; } = null!;

    public UiExpression Expression { get; init; } = null!;
}

public class UiExpression
{
    public UiExpression Parent { get; init; } = null!;

    public RuleSet RuleSet { get; init; } = null!;

    public Expression Expression { get; init; } = null!;

    public (Symbol, UiExpression)? Annotation { get; set; }

    public IEnumerable<ChainPart> GetSuggestions() => RuleSet.GetSuggestions(Expression);
}

public static class UiExpressions
{
    public static UiExpression UiIfy(this Expression expression, UiExpression parent, RuleSet ruleSet)
    {
        var result = new UiExpression { Expression = expression, Parent = parent, RuleSet = ruleSet };

        foreach (var child in expression.Children)
        {
            UiIfy(child, result, ruleSet);
        }

        return result;
    }
}