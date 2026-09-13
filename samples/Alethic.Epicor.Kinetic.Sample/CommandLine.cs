using System;
using System.Collections.Generic;

namespace Alethic.Epicor.Kinetic.Sample;

/// <summary>
/// Minimal argument parser: <c>--name value</c> flags and <c>Name=value</c> parameters.
/// </summary>
sealed class CommandLine
{

    /// <summary>
    /// Parses arguments starting at the given index.
    /// </summary>
    public static CommandLine Parse(string[] args, int start)
    {
        var result = new CommandLine();
        for (var i = start; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.StartsWith("--", StringComparison.Ordinal) && i + 1 < args.Length)
                result._flags[arg[2..]] = args[++i];
            else if (arg.IndexOf('=') is > 0 and var separator)
                result._parameters[arg[..separator]] = arg[(separator + 1)..];
        }

        return result;
    }

    readonly Dictionary<string, string> _flags = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, string> _parameters = new(StringComparer.Ordinal);

    /// <summary>
    /// The <c>Name=value</c> parameters, or <c>null</c> when none were given.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Parameters => _parameters.Count == 0 ? null : _parameters;

    /// <summary>
    /// Returns the value of a <c>--name</c> flag, or <c>null</c> when absent.
    /// </summary>
    public string? Flag(string name)
    {
        return _flags.TryGetValue(name, out var value) ? value : null;
    }

    /// <summary>
    /// Returns a <c>--name</c> flag as an integer, or <c>null</c> when absent or not numeric.
    /// </summary>
    public int? Int(string name)
    {
        return int.TryParse(Flag(name), out var value) ? value : null;
    }

}
