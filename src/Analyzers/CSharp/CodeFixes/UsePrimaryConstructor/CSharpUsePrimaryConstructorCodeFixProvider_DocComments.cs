// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Ignore Spelling: loc kvp

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UsePrimaryConstructor;

using static SyntaxFactory;

internal partial class CSharpUsePrimaryConstructorCodeFixProvider : CodeFixProvider
{
    private const string s_summaryTagName = "summary";
    private const string s_remarksTagName = "remarks";
    private const string s_paramTagName = "param";

    // trivial helpers for working xml doc comments nodes/trivia

    private static SyntaxTrivia GetDocComment(SyntaxNode node)
        => GetDocComment(node.GetLeadingTrivia());

    private static SyntaxTrivia GetDocComment(SyntaxTriviaList trivia)
        => trivia.LastOrDefault(t => t.IsSingleLineDocComment());

    private static DocumentationCommentTriviaSyntax? GetDocCommentStructure(MemberDeclarationSyntax node)
        => GetDocCommentStructure(node.GetLeadingTrivia());

    private static DocumentationCommentTriviaSyntax? GetDocCommentStructure(SyntaxTriviaList trivia)
        => GetDocCommentStructure(GetDocComment(trivia));

    private static DocumentationCommentTriviaSyntax? GetDocCommentStructure(SyntaxTrivia trivia)
        => (DocumentationCommentTriviaSyntax?)trivia.GetStructure();

    private static bool IsXmlElement(XmlNodeSyntax node, string name, [NotNullWhen(true)] out XmlElementSyntax? element)
    {
        element = node is XmlElementSyntax { StartTag.Name.LocalName.ValueText: var elementName } xmlElement && elementName == name
            ? xmlElement
            : null;
        return element != null;
    }

    private static XmlElementSyntax ConvertXmlElementName(XmlElementSyntax xmlElement, string name)
    {
        return xmlElement.ReplaceTokens(
            new[] { xmlElement.StartTag.Name.LocalName, xmlElement.EndTag.Name.LocalName },
            (token, _) => Identifier(name).WithTriviaFrom(token));
    }

    private static SyntaxTriviaList CreateFinalTypeDeclarationLeadingTrivia(
        TypeDeclarationSyntax typeDeclaration,
        ConstructorDeclarationSyntax constructorDeclaration,
        IMethodSymbol constructor,
        ImmutableDictionary<string, string?> properties,
        ImmutableDictionary<ISymbol, (MemberDeclarationSyntax memberNode, SyntaxNode nodeToRemove)> removedMembers)
    {
        // First, take the constructor doc comments and merge those into the type's doc comments.
        // Then, take any removed fields/properties and merge their comments into the type's doc comments.

        var typeDeclarationLeadingTrivia = MergeTypeDeclarationAndConstructorDocComments(typeDeclaration, constructorDeclaration);
        var finalLeadingTrivia = MergeTypeDeclarationAndRemovedMembersDocComments(constructor, properties, removedMembers, typeDeclarationLeadingTrivia);

        return finalLeadingTrivia;

        static IEnumerable<XmlNodeSyntax> ConvertSummaryToParam(IEnumerable<XmlNodeSyntax> content, string parameterName)
        {
            foreach (var node in content)
            {
                yield return IsXmlElement(node, s_summaryTagName, out var xmlElement)
                    ? ConvertXmlElementName(xmlElement, s_paramTagName).AddStartTagAttributes(XmlNameAttribute(CSharpSyntaxFacts.Instance.EscapeIdentifier(parameterName)))
                    : node;
            }
        }

        static IEnumerable<XmlNodeSyntax> ConvertSummaryToRemarks(IEnumerable<XmlNodeSyntax> nodes)
        {
            foreach (var node in nodes)
            {
                yield return IsXmlElement(node, s_summaryTagName, out var xmlElement)
                    ? ConvertXmlElementName(xmlElement, s_remarksTagName)
                    : node;
            }
        }

        static SyntaxTriviaList MergeTypeDeclarationAndConstructorDocComments(
            TypeDeclarationSyntax typeDeclaration,
            ConstructorDeclarationSyntax constructorDeclaration)
        {
            var typeDeclarationLeadingTrivia = typeDeclaration.GetLeadingTrivia();

            // TODO: add support for `/** */` style doc comments if customer demand is there.
            var existingTypeDeclarationDocComment = GetDocComment(typeDeclarationLeadingTrivia);
            var existingConstructorDocComment = GetDocComment(constructorDeclaration);

            if (existingConstructorDocComment == default)
                return typeDeclarationLeadingTrivia;

            // If both type and the constructor have doc comments then merge them.  Otherwise, just move the constructor
            // docs to the type level.
            return InsertOrReplaceDocComments(
                typeDeclarationLeadingTrivia,
                existingTypeDeclarationDocComment == default
                    ? existingConstructorDocComment
                    : MergeDocComments(existingTypeDeclarationDocComment, existingConstructorDocComment));
        }

        static SyntaxTriviaList InsertOrReplaceDocComments(SyntaxTriviaList leadingTrivia, SyntaxTrivia newDocComment)
        {
            newDocComment = newDocComment.WithAdditionalAnnotations(Formatter.Annotation);

            var existingDocComment = GetDocComment(leadingTrivia);
            if (existingDocComment != default)
                return leadingTrivia.Replace(existingDocComment, newDocComment);

            // type doesn't have doc comment, but constructor does.  Move constructor doc comment to type decl.
            // note: the doc comment always ends with a newline.  so we want to place the new one before the
            // final leading spaces of the type decl trivia.
            var insertionIndex = leadingTrivia is [.., (kind: SyntaxKind.WhitespaceTrivia)]
                ? leadingTrivia.Count - 1
                : leadingTrivia.Count;

            return leadingTrivia.Insert(insertionIndex, newDocComment);
        }

        static SyntaxTrivia MergeDocComments(SyntaxTrivia typeDeclarationDocComment, SyntaxTrivia constructorDocComment)
        {
            var typeStructure = GetDocCommentStructure(typeDeclarationDocComment)!;
            var constructorStructure = GetDocCommentStructure(constructorDocComment)!;

            using var _ = ArrayBuilder<XmlNodeSyntax>.GetInstance(out var content);

            // Add all the type decl comments first.
            content.AddRange(typeStructure.Content);

            // then add the constructor comments.  If the type decl already had a summary tag then convert the
            // constructor's summary tag to a 'remarks' tag to keep around the info while not stomping on the
            // existing summary.
            var constructorContents = typeStructure.Content.Any(n => n is XmlElementSyntax { StartTag.Name.LocalName.ValueText: s_summaryTagName })
                ? ConvertSummaryToRemarks(constructorStructure.Content)
                : constructorStructure.Content;

            content.AddRange(constructorContents);

            return Trivia(DocumentationCommentTrivia(
                SyntaxKind.SingleLineDocumentationCommentTrivia,
                List(content),
                typeStructure.EndOfComment));
        }

        static SyntaxTriviaList MergeTypeDeclarationAndRemovedMembersDocComments(
            IMethodSymbol constructor,
            ImmutableDictionary<string, string?> properties,
            ImmutableDictionary<ISymbol, (MemberDeclarationSyntax memberNode, SyntaxNode nodeToRemove)> removedMembers,
            SyntaxTriviaList typeDeclarationLeadingTrivia)
        {
            // now, if we're removing any members, and they had doc comments, and we don't already have doc comments for
            // that parameter in our final doc comment, then move them to there, converting from `<summary>` doc comments to
            // `<param name="x">` doc comments.

            // Keep the <param> tags ordered by the order they are in the constructor parameters.
            var orderedKVPs = properties.OrderBy(kvp => constructor.Parameters.FirstOrDefault(p => p.Name == kvp.Value)?.Ordinal);
            using var _1 = ArrayBuilder<(string parameterName, DocumentationCommentTriviaSyntax docComment)>.GetInstance(out var docCommentsToMove);
            foreach (var (memberName, parameterName) in orderedKVPs)
            {
                var (removedMember, (memberDeclaration, _)) = removedMembers.FirstOrDefault(kvp => kvp.Key.Name == memberName);
                if (removedMember is null)
                    continue;

                var removedMemberDocComment = GetDocCommentStructure(memberDeclaration);
                if (removedMemberDocComment != null)
                    docCommentsToMove.Add((parameterName, removedMemberDocComment)!);
            }

            if (docCommentsToMove.Count == 0)
                return typeDeclarationLeadingTrivia;

            using var _2 = PooledHashSet<string>.GetInstance(out var existingParamNodeNames);
            using var _3 = ArrayBuilder<XmlNodeSyntax>.GetInstance(out var allContent);

            var existingTypeDeclarationDocComment = GetDocCommentStructure(typeDeclarationLeadingTrivia);
            if (existingTypeDeclarationDocComment != null)
            {
                allContent.AddRange(existingTypeDeclarationDocComment.Content);

                foreach (var node in existingTypeDeclarationDocComment.Content)
                {
                    if (IsXmlElement(node, s_paramTagName, out var paramElement))
                    {
                        foreach (var attribute in paramElement.StartTag.Attributes)
                        {
                            if (attribute is XmlNameAttributeSyntax nameAttribute)
                                existingParamNodeNames.Add(nameAttribute.Identifier.Identifier.ValueText);
                        }
                    }
                }
            }

            foreach (var (parameterName, commentToMove) in docCommentsToMove)
            {
                if (!existingParamNodeNames.Contains(parameterName))
                    allContent.AddRange(ConvertSummaryToParam(commentToMove.Content, parameterName));
            }

            return InsertOrReplaceDocComments(
                typeDeclarationLeadingTrivia,
                Trivia(DocumentationCommentTrivia(SyntaxKind.SingleLineDocumentationCommentTrivia, List(allContent))));
        }
    }
}
