namespace Ptt;

public interface IExpressionContext
{
    Boolean IsInteractive => false;

    Boolean AllowsDerivation => false;

    Boolean BeginDerivation(Expression source, Action close, out ChainPart[]? suggestions)
    {
        suggestions = null;

        return false;
    }
}

public class StaticExpressionContext : IExpressionContext
{
    public static IExpressionContext Instance { get; } = new StaticExpressionContext();
}

public class ReasoningChain : IExpressionContext
{
    public Expression Beginning { get; init; } = null!;

    public List<ChainPart> Parts = new List<ChainPart>();

    public RuleSet RuleSet { get; init; }

    public void Add(Symbol symbol, Expression expression)
    {
        Parts.Add(new ChainPart(symbol, expression));
    }

    Boolean IExpressionContext.IsInteractive => true;

    Boolean IExpressionContext.AllowsDerivation => true;

    Boolean IExpressionContext.BeginDerivation(Expression source, Action close, out ChainPart[]? suggestions)
    {
        suggestions = RuleSet.GetSuggestions(source).ToArray();

        return true;
    }
}

public class RuleSet
{
    List<Rule> rules = new List<Rule>();

    public void AddRule(Rule rule) => rules.Add(rule);

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