// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Razor.Utilities;
using Microsoft.AspNetCore.Razor.Utilities.Shared.Resources;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class RazorDiagnostic : IEquatable<RazorDiagnostic>, IFormattable
{
    private readonly RazorDiagnosticDescriptor _descriptor;
    private readonly object[] _args;

    public string Id => _descriptor.Id;
    public RazorDiagnosticSeverity Severity => _descriptor.Severity;
    public int WarningLevel => _descriptor.WarningLevel;
    public SourceSpan Span { get; }

    private Checksum? _checksum;

    private RazorDiagnostic(RazorDiagnosticDescriptor descriptor, SourceSpan? span, object[]? args)
    {
        _descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
        Span = span ?? SourceSpan.Undefined;
        _args = args ?? [];
    }

    public static RazorDiagnostic Create(RazorDiagnosticDescriptor descriptor)
        => new(descriptor, span: null, args: null);

    public static RazorDiagnostic Create(RazorDiagnosticDescriptor descriptor, SourceSpan? span)
        => new(descriptor, span, args: null);

    public static RazorDiagnostic Create(RazorDiagnosticDescriptor descriptor, params object[] args)
        => new(descriptor, span: null, args);

    public static RazorDiagnostic Create(RazorDiagnosticDescriptor descriptor, SourceSpan? span, params object[] args)
        => new(descriptor, span, args);

    internal Checksum Checksum
        => _checksum ??= ComputeChecksum();

    private Checksum ComputeChecksum()
    {
        var builder = new Checksum.Builder();

        builder.Append(_descriptor.Id);
        builder.Append((int)_descriptor.Severity);
        builder.Append(_descriptor.MessageFormat);

        foreach (var arg in _args)
        {
            builder.Append(ConvertEnumIfNeeded(arg));
        }

        var span = Span;
        builder.Append(span.FilePath);
        builder.Append(span.AbsoluteIndex);
        builder.Append(span.LineIndex);
        builder.Append(span.CharacterIndex);
        builder.Append(span.Length);
        builder.Append(span.LineCount);
        builder.Append(span.EndCharacterIndex);

        return builder.FreeAndGetChecksum();

        static object ConvertEnumIfNeeded(object value)
        {
            if (value.GetType() is { IsEnum: true } enumType)
            {
                var underlyingType = Enum.GetUnderlyingType(enumType);

                // Currently, only enums with an underlying type of Int32 are supported,
                // but more can be added if needed.
                if (underlyingType != typeof(int))
                {
                    throw new NotSupportedException(SR.FormatUnsupported_type_0(enumType.FullName));
                }

                return Convert.ToInt32(value);
            }

            return value;
        }
    }

    public string GetMessage()
        => GetMessage(formatProvider: null);

    public string GetMessage(IFormatProvider? formatProvider)
        => string.Format(formatProvider, _descriptor.MessageFormat, _args);

    public override bool Equals(object? obj)
        => obj is RazorDiagnostic diagnostic &&
           Equals(diagnostic);

    public bool Equals(RazorDiagnostic? other)
        => other is not null &&
           Checksum.Equals(other.Checksum);

    public override int GetHashCode()
        => Checksum.GetHashCode();

    public override string ToString()
    {
        return Format(this, formatProvider: null);
    }

    string IFormattable.ToString(string? format, IFormatProvider? formatProvider)
        => Format(this, formatProvider);

    private static string Format(RazorDiagnostic diagnostic, IFormatProvider? formatProvider)
    {
        var span = diagnostic.Span;
        var severity = diagnostic.Severity;
        var id = diagnostic.Id;
        var message = diagnostic.GetMessage(formatProvider);

        // Our indices are 0-based, but we we want to print them as 1-based.
        return $"{span.FilePath}({span.LineIndex + 1},{span.CharacterIndex + 1}): {severity} {id}: {message}";
    }
}
