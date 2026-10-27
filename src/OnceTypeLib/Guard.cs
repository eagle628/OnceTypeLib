namespace OnceTypeLib;

internal static class Guard
{
    internal static void NotNull(object? argument, string paramName)
    {
        if (argument is null)
        {
            throw new ArgumentNullException(paramName);
        }
    }
}
