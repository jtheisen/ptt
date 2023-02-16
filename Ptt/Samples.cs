using static Ptt.RelationSymbols;

namespace Ptt.Samples;

public class SymbolBag
{
    Dictionary<String, Symbol> symbols = new Dictionary<String, Symbol>();

    public Symbol this[String name]
        => symbols.TryGetValue(name, out var symbol) ? symbol : symbols[name] = new Symbol(name);
}

#pragma warning disable CS8618
public class TestVariables
{
    public DslAtomExpression __;

    public DslAtomExpression x, y, z, a, b, c, _0, _1;

    public TestVariables()
    {
        var bag = new SymbolBag();

        foreach (var field in typeof(TestVariables).GetFields())
        {
            var name = field.Name == "__" ? "__" : field.Name.TrimStart('_');

            var expression = new AtomExpression(bag[name]);

            var dslExpression = new DslAtomExpression(expression);

            field.SetValue(this, dslExpression);
        }
    }
}
#pragma warning restore

public class Sample : TestVariables
{
    protected readonly RuleSet ruleSet;

    public Sample()
    {
        ruleSet = new RuleSet();

        ruleSet.AddImplication("neutral", new[] { ~x }, x * _1 == x);
        ruleSet.AddImplication("neutral", new[] { ~x }, _1 * x == x);
        ruleSet.AddImplication("neutral", new[] { ~x }, x + _0 == x);
        ruleSet.AddImplication("neutral", new[] { ~x }, _0 + x == x);
        ruleSet.AddImplication("inverse", new[] { ~x }, x + -x == _0);
        ruleSet.AddImplication("inverse", new[] { ~x }, -x + x == _0);

        ruleSet.AddImplication("commutativity", new[] { ~x, ~y }, x * y == y * x);
        ruleSet.AddImplication("associativity", new[] { ~x, ~y, ~z }, (x * y) * z == x * (y * z));
        ruleSet.AddImplication("commutativity", new[] { ~x, ~y }, x + y == y + x);
        ruleSet.AddImplication("associativity", new[] { ~x, ~y, ~z }, (x + y) + z == x * (y + z));
        ruleSet.AddImplication("distributivity", new[] { ~x, ~y, ~c }, (x + y) * c == x * c + y * c);
        ruleSet.AddImplication("distributivity", new[] { ~x, ~y, ~c }, c * (x + y) == c * x + c * y);

        ruleSet.AddImplication("equality", new[] { ~x, ~y, ~c }, x * c == y * c, x == y);
        ruleSet.AddImplication("equality", new[] { ~x, ~y, ~c }, c * x == c * y, x == y);
        ruleSet.AddImplication("equality", new[] { ~x, ~y, ~c }, x + c == y + c, x == y);
        ruleSet.AddImplication("equality", new[] { ~x, ~y, ~c }, c + x == c + y, x == y);
    }

    public ReasoningChain GetChain()
    {
        var chain = new ReasoningChain { RuleSet = ruleSet, Beginning = a * (x + y) };

        return chain;
    }

    protected void AssertEqual(Expression expected, Expression? actual)
        => ExpressionComparer.AssertEqual(expected, actual);
}
