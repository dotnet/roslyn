// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests;

internal readonly struct RudeEditDiagnosticDescription : IEquatable<RudeEditDiagnosticDescription>
{
    public readonly RudeEditKind RudeEditKind;
    private readonly string? _squiggle;
    private readonly string[] _arguments;

    internal RudeEditDiagnosticDescription(RudeEditKind rudeEditKind, string? squiggle, string[] arguments, string? firstLine)
    {
        RudeEditKind = rudeEditKind;
        _squiggle = squiggle;
        FirstLine = firstLine;
        _arguments = arguments ?? [];
    }

    public string? FirstLine { get; }

    public RudeEditDiagnosticDescription WithFirstLine(string value)
        => new(RudeEditKind, _squiggle, _arguments, value.Trim());

    public bool Equals(RudeEditDiagnosticDescription other)
    {
        return RudeEditKind == other.RudeEditKind
            && _squiggle == other._squiggle
            && (FirstLine == other.FirstLine || FirstLine == null || other.FirstLine == null)
            && _arguments.SequenceEqual(other._arguments, object.Equals);
    }

    public override bool Equals(object? obj)
        => obj is RudeEditDiagnosticDescription && Equals((RudeEditDiagnosticDescription)obj);

    public override int GetHashCode()
    {
        return
            Hash.Combine(_squiggle,
            Hash.CombineValues(_arguments, (int)RudeEditKind));
    }

    public override string ToString()
        => ToString(tryGetResource: null);

    public string ToString(Func<string, string?>? tryGetResource)
    {
        var formattedSquiggle = _squiggle is null
            ? "null"
            : _squiggle.IndexOfAny(['\r', '\n']) >= 0
            ? $""""
            """{Environment.NewLine}{_squiggle}{Environment.NewLine}"""
            """"
            : $"""
            "{_squiggle}"
            """;

        string[] arguments = [formattedSquiggle, .. _arguments.Select(a => tryGetResource?.Invoke(a) is { } ? $"GetResource(\"{a}\")" : $"""
        "{a}"
        """)];
        var withLine = (FirstLine != null) ? $".WithFirstLine(\"{FirstLine}\")" : null;

        return $"Diagnostic(RudeEditKind.{RudeEditKind}, {string.Join(", ", arguments)}){withLine}";
    }

    internal void VerifyMessageFormat()
    {
        var descriptior = EditAndContinueDiagnosticDescriptors.GetDescriptor(RudeEditKind);
        var format = descriptior.MessageFormat.ToString();
        try
        {
            string.Format(format, _arguments);
        }
        catch (FormatException)
        {
            Assert.True(false, $"Message format string was not supplied enough arguments.\nRudeEditKind: {RudeEditKind}\nArguments supplied: {_arguments.Length}\nFormat string: {format}");
        }
    }
}
