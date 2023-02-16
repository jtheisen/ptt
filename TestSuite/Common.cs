using Ptt;

namespace TestSuite;

public class TestsBase : TestVariables
{
    protected void AssertEqual(Expression expected, Expression? actual)
        => ExpressionComparer.AssertEqual(expected, actual);
}

public static class Extensions
{
    public static void TestSuggestions(this AbstractRuleSet ruleSet, Expression expression, Action<ChainPart[]> assertions)
    {
        var suggestions = ruleSet.GetSuggestions(expression).ToArray();

        try
        {
            assertions(suggestions);
        }
        catch (Exception)
        {
            Console.WriteLine("\nAll suggestions:");
            Console.WriteLine(suggestions.Format());

            Console.WriteLine("\nAll rules:");
            Console.WriteLine(ruleSet);

            throw;
        }
    }
}