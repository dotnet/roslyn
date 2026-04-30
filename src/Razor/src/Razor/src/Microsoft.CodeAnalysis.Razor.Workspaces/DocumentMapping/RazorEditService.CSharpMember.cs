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
                BaseMethodDeclarationSyntax method => new(method, GetComparisonSpan(method), sourceText),
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

        private static TextSpan GetComparisonSpan(BaseMethodDeclarationSyntax method)
        {
            // Methods and constructors can be overloaded, so we need enough of the declaration to distinguish overloads,
            // while still ignoring body edits. That keeps unmapped body changes from looking like newly added members.
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
