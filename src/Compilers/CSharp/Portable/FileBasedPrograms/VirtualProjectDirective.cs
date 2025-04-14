// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if NET9_0_OR_GREATER
#pragma warning disable RSEXPERIMENTAL006

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

    public static VirtualProjectDirective Parse(SourceFile sourceFile, TextSpan span, string directiveKind, string directiveText)
    {
        return directiveKind switch
        {
            "sdk" => Sdk.Parse(sourceFile, span, directiveKind, directiveText),
            "property" => Property.Parse(sourceFile, span, directiveKind, directiveText),
            "package" => Package.Parse(sourceFile, span, directiveKind, directiveText),
            _ => throw new DiagnosticException(CSharpResources.UnrecognizedDirective, directiveKind, sourceFile.GetLocationString(span)),
        };
    }

    private static (string, string?) ParseOptionalTwoParts(SourceFile sourceFile, TextSpan span, string directiveKind, string directiveText)
    {
        var i = directiveText.IndexOf(' ', StringComparison.Ordinal);
        var firstPart = checkFirstPart(i < 0 ? directiveText : directiveText[..i]);
        var secondPart = i < 0 ? [] : directiveText.AsSpan(i + 1).TrimStart();
        if (i < 0 || secondPart.IsWhiteSpace())
        {
            return (firstPart, null);
        }

        return (firstPart, secondPart.ToString());

        string checkFirstPart(string firstPart)
        {
            if (string.IsNullOrWhiteSpace(firstPart))
            {
                throw new DiagnosticException(CSharpResources.MissingDirectiveName, directiveKind, sourceFile.GetLocationString(span));
            }

            return firstPart;
        }
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

        public static new Sdk Parse(SourceFile sourceFile, TextSpan span, string directiveKind, string directiveText)
        {
            var (sdkName, sdkVersion) = ParseOptionalTwoParts(sourceFile, span, directiveKind, directiveText);

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

        public static new Property Parse(SourceFile sourceFile, TextSpan span, string directiveKind, string directiveText)
        {
            var (propertyName, propertyValue) = ParseOptionalTwoParts(sourceFile, span, directiveKind, directiveText);

            if (propertyValue is null)
            {
                throw new DiagnosticException(CSharpResources.PropertyDirectiveMissingParts, sourceFile.GetLocationString(span));
            }

            try
            {
                propertyName = XmlConvert.VerifyName(propertyName);
            }
            catch (XmlException ex)
            {
                throw new DiagnosticException(string.Format(CSharpResources.PropertyDirectiveInvalidName, sourceFile.GetLocationString(span), ex.Message), ex);
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

        public static new Package Parse(SourceFile sourceFile, TextSpan span, string directiveKind, string directiveText)
        {
            var (packageName, packageVersion) = ParseOptionalTwoParts(sourceFile, span, directiveKind, directiveText);

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
