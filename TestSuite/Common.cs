namespace TestSuite;

public class TestsBase : TestVariables
{
    protected void AssertEqual(Expression expected, Expression? actual)
        => ExpressionComparer.AssertEqual(expected, actual);
}
