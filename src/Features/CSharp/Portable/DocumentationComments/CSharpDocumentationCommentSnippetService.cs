// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.DocumentationComments;

[ExportLanguageService(typeof(IDocumentationCommentSnippetService), LanguageNames.CSharp), Shared]
internal class CSharpDocumentationCommentSnippetService : AbstractDocumentationCommentSnippetService<DocumentationCommentTriviaSyntax, MemberDeclarationSyntax>
{
    public override string DocumentationCommentCharacter => "/";

    protected override bool AddIndent => true;
    protected override string ExteriorTriviaText => "///";

    private static readonly SymbolDisplayFormat s_format =
        new(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions:
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpDocumentationCommentSnippetService()
    {
    }

    protected override MemberDeclarationSyntax? GetContainingMember(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
    {
        return syntaxTree.GetRoot(cancellationToken).FindToken(position).GetAncestor<MemberDeclarationSyntax>();
    }

    protected override bool SupportsDocumentationComments(MemberDeclarationSyntax member)
    {
        switch (member.Kind())
        {
            case SyntaxKind.ClassDeclaration:
            case SyntaxKind.RecordDeclaration:
            case SyntaxKind.InterfaceDeclaration:
            case SyntaxKind.StructDeclaration:
            case SyntaxKind.RecordStructDeclaration:
            case SyntaxKind.DelegateDeclaration:
            case SyntaxKind.EnumDeclaration:
            case SyntaxKind.EnumMemberDeclaration:
            case SyntaxKind.FieldDeclaration:
            case SyntaxKind.MethodDeclaration:
            case SyntaxKind.ConstructorDeclaration:
            case SyntaxKind.DestructorDeclaration:
            case SyntaxKind.PropertyDeclaration:
            case SyntaxKind.IndexerDeclaration:
            case SyntaxKind.EventDeclaration:
            case SyntaxKind.EventFieldDeclaration:
            case SyntaxKind.OperatorDeclaration:
            case SyntaxKind.ConversionOperatorDeclaration:
                return true;

            default:
                return false;
        }
    }

    protected override bool HasDocumentationComment(MemberDeclarationSyntax member)
        => member.GetFirstToken().LeadingTrivia.Any(t => t is (kind: SyntaxKind.SingleLineDocumentationCommentTrivia or SyntaxKind.MultiLineDocumentationCommentTrivia));

    protected override int GetPrecedingDocumentationCommentCount(MemberDeclarationSyntax member)
    {
        var firstToken = member.GetFirstToken();

        var count = firstToken.LeadingTrivia.Count(t => t.IsDocComment());

        var previousToken = firstToken.GetPreviousToken();
        if (previousToken.Kind() != SyntaxKind.None)
        {
            count += previousToken.TrailingTrivia.Count(t => t.IsDocComment());
        }

        return count;
    }

    protected override List<string> GetDocumentationCommentStubLines(MemberDeclarationSyntax member, string existingCommentText)
    {
        var list = new List<string>
        {
            "/// <summary>",
            "///" + (existingCommentText.StartsWith(" ") ? existingCommentText : $" {existingCommentText}"),
            "/// </summary>"
        };

        var typeParameterList = member.GetTypeParameterList();
        if (typeParameterList != null)
        {
            foreach (var typeParam in typeParameterList.Parameters)
            {
                list.Add("/// <typeparam name=\"" + typeParam.Identifier.ValueText + "\"></typeparam>");
            }
        }

        var parameterList = member.GetParameterList();
        if (parameterList != null)
        {
            foreach (var param in parameterList.Parameters)
            {
                list.Add("/// <param name=\"" + param.Identifier.ValueText + "\"></param>");
            }
        }

        if (member.Kind() is
                SyntaxKind.MethodDeclaration or
                SyntaxKind.IndexerDeclaration or
                SyntaxKind.DelegateDeclaration or
                SyntaxKind.OperatorDeclaration or
                SyntaxKind.ConstructorDeclaration or
                SyntaxKind.DestructorDeclaration)
        {
            var returnType = member.GetMemberType();
            if (returnType != null &&
                !(returnType is PredefinedTypeSyntax predefinedType && predefinedType.Keyword.IsKindOrHasMatchingText(SyntaxKind.VoidKeyword)))
            {
                list.Add("/// <returns></returns>");
            }

            foreach (var exceptionType in GetExceptions(member))
            {
                list.Add(@$"/// <exception cref=""{exceptionType}""></exception>");
            }
        }

        return list;
    }

    private static IEnumerable<string> GetExceptions(SyntaxNode member)
    {
        var throwExpressionsAndStatements = member.DescendantNodes().Where(n => n.Kind() is SyntaxKind.ThrowExpression or SyntaxKind.ThrowStatement);

        var usings = member.GetEnclosingUsingDirectives();
        var hasUsingSystem = usings.Any(u => u.Name is IdentifierNameSyntax { Identifier.ValueText: nameof(System) });

        using var _ = PooledHashSet<string>.GetInstance(out var seenExceptionTypes);
        foreach (var throwExpressionOrStatement in throwExpressionsAndStatements)
        {
            var expression = throwExpressionOrStatement switch
            {
                ThrowExpressionSyntax throwExpression => throwExpression.Expression,
                ThrowStatementSyntax throwStatement => throwStatement.Expression,
                _ => throw ExceptionUtilities.Unreachable()
            };

            if (expression.IsKind(SyntaxKind.NullLiteralExpression))
            {
                // `throw null;` throws NullReferenceException at runtime.
                var exception = hasUsingSystem ? nameof(NullReferenceException) : $"{nameof(System)}.{nameof(NullReferenceException)}";
                if (seenExceptionTypes.Add(exception))
                    yield return exception;
            }
            else if (expression is ObjectCreationExpressionSyntax { Type: TypeSyntax exceptionType })
            {
                exceptionType = exceptionType.ConvertToSingleLine();
                if (!IsExceptionCaughtAndNotRethrown(hasUsingSystem, exceptionType))
                {
                    var exception = exceptionType.ToString();
                    if (seenExceptionTypes.Add(exception))
                        yield return exception.Replace('<', '{').Replace('>', '}');
                }
            }
        }
    }

    private static bool IsExceptionCaughtAndNotRethrown(bool hasUsingSystem, TypeSyntax exceptionType)
    {
        for (SyntaxNode? current = exceptionType; current != null; current = current?.Parent)
        {
            if (current is not BlockSyntax { Parent: TryStatementSyntax tryStatement } block ||
                tryStatement.Block != block ||
                block.DescendantNodes().OfType<ThrowStatementSyntax>().Any(t => t.Expression is null))
            {
                continue;
            }

            foreach (var catchClause in tryStatement.Catches)
            {
                if (catchClause.Filter != null)
                    continue;

                // AN empty `catch { }` will always catch everything.
                if (catchClause.Declaration == null)
                    return true;

                // Poor mans equivalence check since we don't have semantics here.
                if (SyntaxFactory.AreEquivalent(exceptionType, catchClause.Declaration.Type.ConvertToSingleLine()))
                    return true;

                if (hasUsingSystem &&
                    catchClause.Declaration.Type is IdentifierNameSyntax { Identifier.ValueText: nameof(Exception) })
                {
                    return true;
                }

                if (catchClause.Declaration.Type is QualifiedNameSyntax
                    {
                        Left: IdentifierNameSyntax { Identifier.ValueText: nameof(System) },
                        Right: IdentifierNameSyntax { Identifier.ValueText: nameof(Exception) },
                    })
                {
                    return true;
                }
            }
        }

        return false;
    }

    protected override SyntaxToken GetTokenToRight(
        SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
    {
        if (position >= syntaxTree.GetText(cancellationToken).Length)
        {
            return default;
        }

        return syntaxTree.GetRoot(cancellationToken).FindTokenOnRightOfPosition(
            position, includeDirectives: true, includeDocumentationComments: true);
    }

    protected override SyntaxToken GetTokenToLeft(
        SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
    {
        if (position < 1)
        {
            return default;
        }

        return syntaxTree.GetRoot(cancellationToken).FindTokenOnLeftOfPosition(
            position - 1, includeDirectives: true, includeDocumentationComments: true, includeSkipped: true);
    }

    protected override bool IsDocCommentNewLine(SyntaxToken token)
        => token.RawKind == (int)SyntaxKind.XmlTextLiteralNewLineToken;

    protected override bool IsEndOfLineTrivia(SyntaxTrivia trivia)
        => trivia.RawKind == (int)SyntaxKind.EndOfLineTrivia;

    protected override bool IsSingleExteriorTrivia(DocumentationCommentTriviaSyntax documentationComment, [NotNullWhen(true)] out string? existingCommentText)
    {
        existingCommentText = null;

        if (IsMultilineDocComment(documentationComment))
        {
            return false;
        }

        if (documentationComment.Content.Count != 1)
        {
            return false;
        }

        if (documentationComment.Content[0] is not XmlTextSyntax xmlText)
        {
            return false;
        }

        var textTokens = xmlText.TextTokens;
        if (!textTokens.Any())
        {
            return false;
        }

        var lastTextToken = textTokens.Last();
        var firstTextToken = textTokens.First();

        // We only allow more than one token if the first one is an actual comment, not whitespace
        if (textTokens.Count != 1 && string.IsNullOrWhiteSpace(firstTextToken.ValueText))
        {
            return false;
        }

        // If there are two text tokens it means there is an existing comment that we want to
        // preserve.
        existingCommentText = textTokens.Count == 1 ? "" : firstTextToken.ValueText;

        return lastTextToken.Kind() == SyntaxKind.XmlTextLiteralNewLineToken
            && lastTextToken.TrailingTrivia.Count == 0
            && firstTextToken.LeadingTrivia is [(kind: SyntaxKind.DocumentationCommentExteriorTrivia) firstTrivia]
            && firstTrivia.ToString() == ExteriorTriviaText;
    }

    private static IList<SyntaxToken> GetTextTokensFollowingExteriorTrivia(XmlTextSyntax xmlText)
    {
        var result = new List<SyntaxToken>();

        var tokenList = xmlText.TextTokens;
        foreach (var token in tokenList.Reverse())
        {
            result.Add(token);

            if (token.LeadingTrivia.Any(SyntaxKind.DocumentationCommentExteriorTrivia))
            {
                break;
            }
        }

        result.Reverse();

        return result;
    }

    protected override bool EndsWithSingleExteriorTrivia(DocumentationCommentTriviaSyntax? documentationComment)
    {
        if (documentationComment == null)
        {
            return false;
        }

        if (IsMultilineDocComment(documentationComment))
        {
            return false;
        }

        if (documentationComment.Content.LastOrDefault() is not XmlTextSyntax xmlText)
        {
            return false;
        }

        var textTokens = GetTextTokensFollowingExteriorTrivia(xmlText);

        if (textTokens.Any(t => !string.IsNullOrWhiteSpace(t.ToString())))
        {
            return false;
        }

        var lastTextToken = textTokens.LastOrDefault();
        var firstTextToken = textTokens.FirstOrDefault();

        return lastTextToken.Kind() == SyntaxKind.XmlTextLiteralNewLineToken
            && firstTextToken.LeadingTrivia.Count == 1
            && firstTextToken.LeadingTrivia.ElementAt(0).Kind() == SyntaxKind.DocumentationCommentExteriorTrivia
            && firstTextToken.LeadingTrivia.ElementAt(0).ToString() == ExteriorTriviaText
            && lastTextToken.TrailingTrivia.Count == 0;
    }

    protected override bool IsMultilineDocComment(DocumentationCommentTriviaSyntax? documentationComment)
        => documentationComment.IsMultilineDocComment();

    protected override bool HasSkippedTrailingTrivia(SyntaxToken token)
        => token.TrailingTrivia.Any(t => t.Kind() == SyntaxKind.SkippedTokensTrivia);
}
