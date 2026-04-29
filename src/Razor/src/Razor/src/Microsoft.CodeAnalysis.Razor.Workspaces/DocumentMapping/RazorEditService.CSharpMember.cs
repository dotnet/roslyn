// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Razor.DocumentMapping;

internal partial class RazorEditService
{
    private sealed class CSharpMember : IEquatable<CSharpMember>
    {
        private readonly MemberDeclarationSyntax _member;
        private readonly TextSpan _comparisonSpan;

        public SourceText Text { get; }

        private CSharpMember(MemberDeclarationSyntax member, TextSpan comparisonSpan, SourceText text)
        {
            _member = member;
            _comparisonSpan = comparisonSpan;
            Text = text;
        }

        public static CSharpMember? TryCreate(MemberDeclarationSyntax member, SourceText sourceText)
            => member switch
            {
                MethodDeclarationSyntax method => new(method, GetComparisonSpan(method), sourceText),
                PropertyDeclarationSyntax property => new(property, GetComparisonSpan(property), sourceText),
                FieldDeclarationSyntax field => new(field, GetComparisonSpan(field), sourceText),
                _ => null,
            };

        public bool Equals(CSharpMember? other)
        {
            if (other is null || _member.RawKind != other._member.RawKind)
            {
                return false;
            }

            return Text.NonWhitespaceContentEquals(other.Text, _comparisonSpan.Start, _comparisonSpan.End, other._comparisonSpan.Start, other._comparisonSpan.End);
        }

        public override bool Equals(object? obj)
            => Equals(obj as CSharpMember);

        public override int GetHashCode()
        {
            // Given the gymnastics we are doing to construct a modified generated document, we want to always fallback to the Equals check
            // as that is the only actual trustworthy comparison we can do. Constructing a string from the source text without whitespace just
            // to get the hashcode seems like overkill for the amount of members we expect to be added/removed in a typical code action.
            return 0;
        }

        // We don't want trivia, because it will include generated artifacts like #line directives, so using Span instead of FullSpan in the two
        // methods below is deliberate
        public int GetStartLineNumber()
            => Text.Lines.GetLineFromPosition(_member.SpanStart).LineNumber;

        public int GetEndLineNumber()
            => Text.Lines.GetLineFromPosition(Math.Max(_member.SpanStart, _member.Span.End - 1)).LineNumber;

        private static TextSpan GetComparisonSpan(MethodDeclarationSyntax method)
        {
            // Since we only want to know about additions, we need to ignore any body changes, so we end our comparison span
            // before the body, or expression body, starts. This prevents changes inside method bodies that are entirely unmapped
            // causing us to add that method. Since an existing unmapped method can only be present if the Razor compiler emitted
            // it, we never want those in the Razor file.
            // Strictly speaking this is comparing more than necessary - since a C# method can't be overloaded by return type for
            // example, having that as part of the comparison is redundant. Same for visibility modifiers, which would seem to show
            // a bug in this logic: If Roslyn changes a method from public to private via a code action, that would appear to this
            // logic as an addition. In reality though, such a change would have to be in a mappable region to be a valid code action,
            // so the edits will have been processed already, and not seen by this code. For a method to go from public to private
            // in an unmappable region means Roslyn is changing one of the Razor compiler generated methods, which the user can
            // never see or interact with.
            // If the user has an incomplete method, then we are safe to just use the end of the method node.
            if (((SyntaxNode?)method.Body ?? method.ExpressionBody)?.SpanStart is not { } spanEnd)
            {
                spanEnd = method.Span.End;
            }

            return TextSpan.FromBounds(method.SpanStart, spanEnd);
        }

        private static TextSpan GetComparisonSpan(PropertyDeclarationSyntax property)
        {
            // Properties can't be overloaded, so the name alone is enough to tell whether a generated property
            // already exists. Keeping the comparison this narrow avoids treating accessor, initializer, or
            // modifier changes as additions.
            return property.Identifier.Span;
        }

        private static TextSpan GetComparisonSpan(FieldDeclarationSyntax field)
        {
            // Fields can't be overloaded either, so the declared variable name(s) are enough to identify an
            // existing generated field. Comparing only that span avoids treating modifier or initializer changes
            // as additions.
            var variables = field.Declaration.Variables;
            if (variables.Count == 0)
            {
                return field.Declaration.Span;
            }

            return TextSpan.FromBounds(variables[0].Identifier.SpanStart, variables[^1].Identifier.Span.End);
        }
    }
}
