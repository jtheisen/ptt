namespace Ptt;

public class ExpressionDiscrepancyException : Exception
{
    public ExpressionDiscrepancyException(String message)
        : base(message)
    {
    }
}

public class ExpressionComparer
{
    public void Compare(Expression expected, Expression actual)
    {
        if (expected.GetType() != actual.GetType())
        {
            Error($"Unequal types, got {actual.GetType()}, expected {expected.GetType()}");
        }

        if (expected is AtomExpression ea && actual is AtomExpression aa)
        {
            Compare(ea, aa);
        }
        else if (expected is BinaryExpression eb && actual is BinaryExpression ab)
        {
            Compare(eb, ab);
        }
        else if (expected is UnaryExpression eu && actual is UnaryExpression au)
        {
            Compare(eu, au);
        }
        else
        {
            throw new Exception($"Unexpected expression type {actual.GetType()}");
        }
    }

    void Compare(AtomExpression expected, AtomExpression actual)
    {
        if (expected.Symbol != actual.Symbol) Error($"Symbols are different, got {actual.Symbol}, expected {expected.Symbol}");
    }

    void Compare(BinaryExpression expected, BinaryExpression actual)
    {
        if (expected.Symbol != actual.Symbol) Error($"Symbols are different, got {actual.Symbol}, expected {expected.Symbol}");

        Compare(expected.Lhs, actual.Lhs);
        Compare(expected.Rhs, actual.Rhs);
    }

    void Compare(UnaryExpression expected, UnaryExpression actual)
    {
        if (expected.Symbol != actual.Symbol) Error($"Symbols are different, got {actual.Symbol}, expected {expected.Symbol}");

        Compare(expected.Op, actual.Op);
    }

    void Error(String message)
    {
        throw new ExpressionDiscrepancyException(message);
    }

    public static void AssertEqual(Expression expected, Expression? actual)
    {
        if (actual is null) throw new Exception($"Actual result was null");

        var comparer = new ExpressionComparer();

        comparer.Compare(expected, actual);
    }
}
