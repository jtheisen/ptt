namespace TestSuite;

[TestClass]
public class RulesTests : TestVariables
{
    RuleSet ruleSet = new SampleRuleSet().ruleSet;

    [TestMethod]
    public void TestBasics() => ruleSet.TestSuggestions(a * b, s =>
    {
        Assert.IsTrue(s.Contains((__ == b * a).ToChainPart()));
        Assert.IsTrue(s.Contains((__ == a * b + _0).ToChainPart()));
    });

    [TestMethod]
    public void TestEquality() => ruleSet.Reduce(a == c).TestSuggestions(a * b, s =>
    {
        Assert.IsTrue(s.Contains((__ == c * b).ToChainPart()));
    });
}
