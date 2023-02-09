namespace TestSuite;

[TestClass]
public class UnificationTests : TestsBase
{
    [TestMethod]
    public void TestBasics()
    {
        Assert.ThrowsException<ExpressionDiscrepancyException>(() => AssertEqual(x * y, y * x));
        AssertEqual(x * y, x * y);
    }

    [TestMethod]
    public void TestRules()
    {
        var rule = new Rule("comm", new[] { ~x, ~y }, SymEq, x * y, y * x);

        AssertEqual(
            b * a,
            rule.ApplyRule(a * b)!.Expression
            );
    }
}
