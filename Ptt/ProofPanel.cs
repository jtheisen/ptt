using System.Diagnostics.CodeAnalysis;

namespace Ptt;

public interface IExpressionContext
{
    Boolean IsInteractive => false;

    Boolean AllowsDerivation => false;

    Boolean CanDerive(UiExpression expression) => false;

    Boolean BeginDerivation(UiExpression source, Action close, out ChainPart[]? suggestions)
    {
        suggestions = null;

        return false;
    }

    void SetAnnotation(UiExpression target, ChainPart selection) { }

    UiReasoningState? GetReasoningState() => null;
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

    public UiReasoningState ReasoningState { get; set; }



    public void Add(Symbol symbol, Expression expression)
    {
        expression.UiIfy(null);
        Parts.Add(new ChainPart(symbol, expression));
    }

    Boolean IExpressionContext.IsInteractive => true;

    Boolean IExpressionContext.AllowsDerivation => true;

    Boolean IExpressionContext.CanDerive(UiExpression expression)
        => ReasoningState.CanSetAnnotation(expression);

    Boolean IExpressionContext.BeginDerivation(UiExpression source, Action close, out ChainPart[]? suggestions)
    {
        var subExpressions = source.Expression.Children.Select(c => c.Ui).OfType<UiExpression>().ToArray();

        //var subRelations = subExpressions.Select  (c => c.ann)

        suggestions = RuleSet.GetSuggestions(source.Expression).ToArray();

        ReasoningState.SetCloseChooser(close);

        return true;
    }

    void IExpressionContext.SetAnnotation(UiExpression target, ChainPart choice)
    {
        ReasoningState.SetAnnotation(this, target, choice);
    }

    UiReasoningState? IExpressionContext.GetReasoningState() => ReasoningState;
}

public class UiChainPart
{
    public Symbol Symbol { get; init; } = null!;

    public UiExpression Expression { get; init; } = null!;
}

public class UiExpression
{
    public UiExpression? Parent { get; init; }

    //public RuleSet RuleSet { get; init; } = null!;

    public Expression Expression { get; init; } = null!;

    public Boolean IsFirstOrOnly => Parent?.Expression is BinaryExpression be ? be.Lhs == Expression : true;

    public UiAnnotation? Annotation { get; set; }

    //public IEnumerable<ChainPart> GetSuggestions() => RuleSet.GetSuggestions(Expression);
}

public interface IComponentNotifier
{
    void Notify(Object expression);
}

public class UiReasoningState
{
    private readonly IComponentNotifier notifier;

    ReasoningChain? chain;

    Action? closeChooser;

    UiExpression? lhs, rhs;

    public UiReasoningState(IComponentNotifier notifier)
    {
        this.notifier = notifier;
    }

    public void CloseChooser()
    {
        closeChooser?.Invoke();
        closeChooser = null;
    }

    public void SetCloseChooser(Action closeChooser)
    {
        CloseChooser();
        this.closeChooser = closeChooser;
    }

    public Boolean CanSetAnnotation(UiExpression expression)
    {
        if (lhs is null && rhs is null) return true;

        if (lhs is not null && rhs is not null) return false;

        var existing = (lhs ?? rhs)!;

        if (existing == expression) return false;

        if (existing.Parent != expression.Parent) return false;

        return true;
    }

    public void SetAnnotation(ReasoningChain chain, UiExpression expression, ChainPart choice)
    {
        if (!CanSetAnnotation(expression)) return;

        this.chain = chain;

        expression.Annotation = new UiAnnotation(expression.IsFirstOrOnly, choice);

        if (expression.IsFirstOrOnly)
        {
            lhs = expression;
        }
        else
        {
            rhs = expression;
        }
    }

    public void HandleEscape()
    {
        if (closeChooser is not null)
        {
            CloseChooser();
        }
        else if (rhs is not null)
        {
            Reset(ref rhs);
            ResetChainIfApplicable();
        }
        else if (lhs is not null)
        {
            Reset(ref lhs);
            ResetChainIfApplicable();
        }
    }

    public void HandleSpace()
    {
        if (rhs is not null) return;

        if (lhs is null) return;

        if (lhs.Parent is not null) return;

        if (lhs.Annotation?.ChainPart is not ChainPart part) return;

        if (chain is null) return;

        chain.Add(part.Symbol, part.Expression);

        notifier.Notify(chain);

        Reset(ref rhs);
        Reset(ref lhs);
        ResetChainIfApplicable();
    }

    void Reset(ref UiExpression? expression)
    {
        if (expression is null) return;
        var backup = expression;
        expression.Annotation = null;
        expression = null;
        notifier.Notify(backup);
    }

    void ResetChainIfApplicable()
    {
        if (rhs is null && lhs is null && chain is not null)
        {
            var backup = chain;
            chain = null;
            notifier.Notify(backup);
        }
    }
}

public record UiAnnotation(Boolean IsTopOrLeft, ChainPart ChainPart);

public static class UiExpressions
{
    // FIXME: this should also clone to undo subterm sharing
    public static UiExpression UiIfy(this Expression expression, UiExpression? parent/*, RuleSet ruleSet*/)
    {
        var result = new UiExpression { Expression = expression, Parent = parent/*, RuleSet = ruleSet*/ };

        expression.Ui = result;

        foreach (var child in expression.Children)
        {
            UiIfy(child, result/*, ruleSet*/);
        }

        return result;
    }
}