namespace Ptt;

public class ReasoningChain
{
    public Expression Beginning { get; private set; } = null!;

    public List<ChainPart> Parts = new List<ChainPart>();

    public AbstractRuleSet RuleSet { get; init; }

    public ReasoningChain(AbstractRuleSet ruleSet, Expression beginning)
    {
        Beginning = beginning.UiIfy(this).Expression;
        RuleSet = ruleSet;
    }

    public void Add(Symbol symbol, Expression expression)
    {
        Parts.Add(new ChainPart(symbol, expression.UiIfy(this).Expression));
    }
}

public class UiChainPart
{
    public Symbol Symbol { get; init; } = null!;

    public UiExpression Expression { get; init; } = null!;
}

public class UiExpression
{
    public ReasoningChain Chain { get; init; }

    public UiExpression? Parent { get; init; }

    //public RuleSet RuleSet { get; init; } = null!;

    public Expression Expression { get; init; } = null!;

    public Boolean IsFirstOrOnly => Parent?.Expression is BinaryExpression be ? be.Lhs == Expression : true;

    public RuleSet? RuleSet { get; set; }

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

    public void SetAnnotation(UiExpression expression, ChainPart choice)
    {
        if (!CanSetAnnotation(expression)) return;

        choice.Expression.AssertNonUi();

        var chain = expression.Chain;

        if (this.chain is null)
        {
            this.chain = chain;
        }
        else if (this.chain != chain)
        {
            throw new Exception($"Invalid chain");
        }

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

    public Boolean BeginDerivation(UiExpression source, Action close, out ChainPart[]? suggestions)
    {
        suggestions = null;

        var subExpressions = source.Expression.Children.Select(c => c.Ui).OfType<UiExpression>().ToArray();

        //var subRelations = subExpressions.Select  (c => c.ann)

        var ruleSet = GetRuleset(source);

        if (ruleSet is null) return false;

        suggestions = ruleSet.GetSuggestions(source.Expression.MakeClone().AssertNonUi()).ToArray();

        SetCloseChooser(close);

        return true;
    }

    AbstractRuleSet? GetRuleset(UiExpression source)
    {
        if (this.chain is not null && this.chain != source.Chain) throw new Exception($"Invalid chain");

        var reduced = source.Chain.RuleSet;

        if (reduced is null) return null;

        var children = source.Expression.Children
            .Select(c => c.Ui)
            .OfType<UiExpression>()
            .ToArray();

        foreach (var child in children)
        {
            if (child is UiExpression ui && ui.Annotation is UiAnnotation annotation)
            {
                reduced = reduced.Reduce(annotation.ChainPart.ToRelation(ui.Expression.MakeClone()));
            }
        }

        return reduced;
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
    public static UiExpression UiIfy(this Expression expression, ReasoningChain chain)
    {
        if (expression.Ui is UiExpression otherUi)
        {
            if (otherUi.Chain != chain) throw new Exception($"Expression already rooted in a different chain");

            return otherUi;
        }

        var clone = expression.MakeClone();
        clone.UiIfyInSitu(chain, null);
        return (UiExpression)clone.Ui!;
    }

    public static void UiIfyInSitu(this Expression expression, ReasoningChain chain, UiExpression? parent/*, RuleSet ruleSet*/)
    {
        var result = new UiExpression { Expression = expression, Chain = chain, Parent = parent/*, RuleSet = ruleSet*/ };

        expression.Ui = result;

        foreach (var child in expression.Children)
        {
            UiIfyInSitu(child, chain, result/*, ruleSet*/);
        }
    }
}