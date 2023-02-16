public static class Extensions
{
    public static IEnumerable<T> Singleton<T>(this T instance) => new[] { instance };
}
