using System;

namespace Alethic.Epicor.Kinetic.Client.Internal;

/// <summary>
/// Argument checks that work on every target framework.
/// </summary>
static class Guard
{

    /// <summary>
    /// Throws when the value is null, empty or whitespace; otherwise returns it.
    /// </summary>
    public static string NotNullOrWhiteSpace(string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or whitespace.", paramName);

        return value!;
    }

    /// <summary>
    /// Throws when the value is null; otherwise returns it.
    /// </summary>
    public static T NotNull<T>(T? value, string paramName)
        where T : class
    {
        return value ?? throw new ArgumentNullException(paramName);
    }

}
