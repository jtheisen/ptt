namespace TestSuite;

[TestClass]
public class RulesTests : Sample
{
    [TestMethod]
    public void TestBasics()
    {
        var rs = RuleSet.Build(r => r.AddImplication("commutativity", new[] { ~x, ~y }, x * y == y * x));

        var suggestions = rs.GetSuggestions(a * b).ToArray();

        Console.WriteLine(suggestions.Format());

        Assert.IsTrue(suggestions.Contains((__ == b * a).ToChainPart()));
    }
}
