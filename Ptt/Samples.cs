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
    public AtomExpression x, y, z, a, b, c, _0, _1;

    public TestVariables()
    {
        var bag = new SymbolBag();

        foreach (var field in typeof(TestVariables).GetFields())
        {
            field.SetValue(this, new AtomExpression(bag[field.Name.TrimStart('_')]));
        }
    }
}
#pragma warning restore

public class Sample : TestVariables
{
    public ReasoningChain GetChain()
    {
        var ruleSet = new RuleSet();

        ruleSet.AddRule(new Rule("neutral", new[] { ~x }, SymEq, x * _1, x));
        ruleSet.AddRule(new Rule("neutral", new[] { ~x }, SymEq, _1 * x, x));

        ruleSet.AddRule(new Rule("neutral", new[] { ~x }, SymEq, x + _0, x));
        ruleSet.AddRule(new Rule("neutral", new[] { ~x }, SymEq, _0 + x, x));

        ruleSet.AddRule(new Rule("commutativity", new[] { ~x, ~y }, SymEq, x * y, y * x));
        ruleSet.AddRule(new Rule("associativity", new[] { ~x, ~y, ~z }, SymEq, (x * y) * z, x * (y * z)));

        ruleSet.AddRule(new Rule("commutativity", new[] { ~x, ~y }, SymEq, x + y, y + x));
        ruleSet.AddRule(new Rule("associativity", new[] { ~x, ~y, ~z }, SymEq, (x + y) + z, x * (y + z)));

        ruleSet.AddRule(new Rule("distributivity", new[] { ~x, ~y, ~z }, SymEq, (x + y) * c, x * c + y * c));
        ruleSet.AddRule(new Rule("distributivity", new[] { ~x, ~y, ~z }, SymEq, c * (x + y), c * x + c * y));

        var chain = new ReasoningChain { RuleSet = ruleSet, Beginning = x * y * z };

        chain.Add(SymLt, x * (y * z));

        return chain;
    }
}
