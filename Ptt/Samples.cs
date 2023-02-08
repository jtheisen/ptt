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
    public AtomExpression x, y, z, a, b, c;

    public TestVariables()
    {
        var bag = new SymbolBag();

        foreach (var field in typeof(TestVariables).GetFields())
        {
            field.SetValue(this, new AtomExpression(bag[field.Name]));
        }
    }
}
#pragma warning restore

public class Sample : TestVariables
{
    public ReasoningChain GetChain()
    {
        var chain = new ReasoningChain { Beginning = x * y * z };

        chain.Add(SymLt, x * (y * z));

        return chain;
    }
}
