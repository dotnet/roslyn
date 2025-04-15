// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET9_0_OR_GREATER

using System;
using System.Xml;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.FileBasedPrograms;

/// <summary>
/// Represents a C# directive starting with <c>#:</c>.
/// Those are ignored by the language but recognized by file-based programs.
/// </summary>
internal abstract class VirtualProjectDirective
{
    private VirtualProjectDirective() { }

    /// <summary>
    /// Span of the full line including the trailing line break.
    /// </summary>
    public required TextSpan Span { get; init; }

    /// <param name="span">
    /// See <see cref="Span"/>. This is the full span that will be removed during file-based to project-based conversion
    /// unlike in <paramref name="locationInfo"/> which is only used for diagnostics and can contain any user-friendly span.
    /// </param>
    public static VirtualProjectDirective? TryParse(in LocationInfo locationInfo, TextSpan span, string directiveKind, string directiveText, DiagnosticBag diagnostics)
    {
        switch (directiveKind)
        {
            case "sdk": return Sdk.TryParseOne(locationInfo, span, directiveKind, directiveText, diagnostics);
            case "property": return Property.TryParseOne(locationInfo, span, directiveKind, directiveText, diagnostics);
            case "package": return Package.TryParseOne(locationInfo, span, directiveKind, directiveText, diagnostics);
            default:
                diagnostics.Add(ErrorCode.ERR_UnrecognizedDirective, locationInfo.ToLocation(), directiveKind);
                return null;
        }
    }

    private static (string, string?)? TryParseOptionalTwoParts(in LocationInfo locationInfo, string directiveKind, string directiveText, DiagnosticBag diagnostics)
    {
        var i = directiveText.IndexOf(' ', StringComparison.Ordinal);
        var firstPart = i < 0 ? directiveText : directiveText[..i];

        if (string.IsNullOrWhiteSpace(firstPart))
        {
            diagnostics.Add(ErrorCode.ERR_MissingDirectiveName, locationInfo.ToLocation(), directiveKind);
            return null;
        }

        var secondPart = i < 0 ? [] : directiveText.AsSpan(i + 1).TrimStart();
        if (i < 0 || secondPart.IsWhiteSpace())
        {
            return (firstPart, null);
        }

        return (firstPart, secondPart.ToString());
    }

    /// <summary>
    /// <c>#!</c> directive.
    /// </summary>
    public sealed class Shebang : VirtualProjectDirective;

    /// <summary>
    /// <c>#:sdk</c> directive.
    /// </summary>
    public sealed class Sdk : VirtualProjectDirective
    {
        private Sdk() { }

        public required string Name { get; init; }
        public string? Version { get; init; }

        public static Sdk? TryParseOne(in LocationInfo locationInfo, TextSpan span, string directiveKind, string directiveText, DiagnosticBag diagnostics)
        {
            var parts = TryParseOptionalTwoParts(locationInfo, directiveKind, directiveText, diagnostics);

            if (parts is not var (sdkName, sdkVersion))
            {
                return null;
            }

            return new Sdk
            {
                Span = span,
                Name = sdkName,
                Version = sdkVersion,
            };
        }

        public string ToSlashDelimitedString()
        {
            return Version is null ? Name : $"{Name}/{Version}";
        }
    }

    /// <summary>
    /// <c>#:property</c> directive.
    /// </summary>
    public sealed class Property : VirtualProjectDirective
    {
        private Property() { }

        public required string Name { get; init; }
        public required string Value { get; init; }

        public static Property? TryParseOne(in LocationInfo locationInfo, TextSpan span, string directiveKind, string directiveText, DiagnosticBag diagnostics)
        {
            var parts = TryParseOptionalTwoParts(locationInfo, directiveKind, directiveText, diagnostics);

            if (parts is not var (propertyName, propertyValue))
            {
                return null;
            }

            if (propertyValue is null)
            {
                diagnostics.Add(ErrorCode.ERR_PropertyDirectiveMissingParts, locationInfo.ToLocation());
                return null;
            }

            try
            {
                propertyName = XmlConvert.VerifyName(propertyName);
            }
            catch (XmlException ex)
            {
                diagnostics.Add(ErrorCode.ERR_PropertyDirectiveInvalidName, locationInfo.ToLocation(), ex.Message);
                return null;
            }

            return new Property
            {
                Span = span,
                Name = propertyName,
                Value = propertyValue,
            };
        }
    }

    /// <summary>
    /// <c>#:package</c> directive.
    /// </summary>
    public sealed class Package : VirtualProjectDirective
    {
        private Package() { }

        public required string Name { get; init; }
        public string? Version { get; init; }

        public static Package? TryParseOne(in LocationInfo locationInfo, TextSpan span, string directiveKind, string directiveText, DiagnosticBag diagnostics)
        {
            var parts = TryParseOptionalTwoParts(locationInfo, directiveKind, directiveText, diagnostics);

            if (parts is not var (packageName, packageVersion))
            {
                return null;
            }

            return new Package
            {
                Span = span,
                Name = packageName,
                Version = packageVersion,
            };
        }
    }
}

#endif
