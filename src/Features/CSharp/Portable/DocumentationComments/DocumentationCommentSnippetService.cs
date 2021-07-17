// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Simplification.Simplifiers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.DocumentationComments
{
    [ExportLanguageService(typeof(IDocumentationCommentSnippetService), LanguageNames.CSharp), Shared]
    internal class DocumentationCommentSnippetService : AbstractDocumentationCommentSnippetService<DocumentationCommentTriviaSyntax, MemberDeclarationSyntax>
    {
        public override string DocumentationCommentCharacter => "/";

        protected override bool AddIndent => true;
        protected override string ExteriorTriviaText => "///";

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DocumentationCommentSnippetService()
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
            => member.GetFirstToken().LeadingTrivia.Any(SyntaxKind.SingleLineDocumentationCommentTrivia, SyntaxKind.MultiLineDocumentationCommentTrivia);

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

        protected override bool IsMemberDeclaration(MemberDeclarationSyntax member)
            => true;

        protected override List<string> GetDocumentationCommentStubLines(MemberDeclarationSyntax member, SemanticModel model, OptionSet options, CancellationToken cancellationToken)
        {
            var list = new List<string>
            {
                "/// <summary>",
                "/// ",
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

            if (member.IsKind(
                    SyntaxKind.MethodDeclaration,
                    SyntaxKind.IndexerDeclaration,
                    SyntaxKind.DelegateDeclaration,
                    SyntaxKind.OperatorDeclaration,
                    SyntaxKind.ConstructorDeclaration,
                    SyntaxKind.DestructorDeclaration))
            {
                var returnType = member.GetMemberType();
                if (returnType != null &&
                    !(returnType.IsKind(SyntaxKind.PredefinedType, out PredefinedTypeSyntax? predefinedType) && predefinedType.Keyword.IsKindOrHasMatchingText(SyntaxKind.VoidKeyword)))
                {
                    list.Add("/// <returns></returns>");
                }

                foreach (var exceptionType in GetExceptions(member, model, options, cancellationToken))
                {
                    list.Add(@$"/// <exception cref=""{exceptionType}""></exception>");
                }
            }

            return list;
        }

        private static IEnumerable<string> GetExceptions(SyntaxNode member, SemanticModel model, OptionSet options, CancellationToken cancellationToken)
        {
            var throwExpressionsAndStatements = member.DescendantNodes().Where(n => n.IsKind(SyntaxKind.ThrowExpression, SyntaxKind.ThrowStatement));
            var hasUsingSystem = model.GetUsingNamespacesInScope(member).Contains(n => n.ContainingNamespace.IsGlobalNamespace && n.Name == "System");
            foreach (var throwExpressionOrStatement in throwExpressionsAndStatements)
            {
                var expression = throwExpressionOrStatement switch
                {
                    ThrowExpressionSyntax throwExpression => throwExpression.Expression,
                    ThrowStatementSyntax throwStatement => throwStatement.Expression,
                    _ => throw ExceptionUtilities.Unreachable
                };

                if (expression.IsKind(SyntaxKind.NullLiteralExpression))
                {
                    // `throw null;` throws NullReferenceException at runtime.
                    if (hasUsingSystem)
                    {
                        yield return nameof(NullReferenceException);
                    }
                    else
                    {
                        yield return $"{nameof(System)}.{nameof(NullReferenceException)}";
                    }
                }
                else if (expression is not null)
                {
                    var type = model.GetTypeInfo(expression, cancellationToken).Type;
                    if (type is not null && !IsExceptionCaughtAndNotRethrown(throwExpressionOrStatement, type, model, cancellationToken))
                    {
                        var nameSyntax = type.GenerateTypeSyntax();
                        yield return nameSyntax.ToString().Replace('<', '{').Replace('>', '}');
                    }
                }
            }
        }

        private static bool IsExceptionCaughtAndNotRethrown(SyntaxNode throwExpressionOrStatement, ITypeSymbol exceptionType, SemanticModel model, CancellationToken cancellationToken)
        {
            var tryStatement = throwExpressionOrStatement.FirstAncestorOrSelfUntil<TryStatementSyntax>(n => n is CatchClauseSyntax or LocalFunctionStatementSyntax or MemberDeclarationSyntax);
            if (tryStatement is null || tryStatement.Catches.Count == 0)
            {
                return false;
            }

            foreach (var catchClause in tryStatement.Catches)
            {
                if (catchClause.Filter is not null)
                {
                    // It's possible that exception isn't caught.
                    continue;
                }

                if (model.GetOperation(catchClause, cancellationToken) is ICatchClauseOperation catchClauseOperation)
                {
                    if (exceptionType.InheritsFromOrEquals(catchClauseOperation.ExceptionType))
                    {
                        // TODO: Check not rethrown.
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

        protected override bool IsSingleExteriorTrivia(DocumentationCommentTriviaSyntax documentationComment, bool allowWhitespace = false)
        {
            if (IsMultilineDocComment(documentationComment))
            {
                return false;
            }

            if (documentationComment.Content.Count != 1)
            {
                return false;
            }

            if (!(documentationComment.Content[0] is XmlTextSyntax xmlText))
            {
                return false;
            }

            var textTokens = xmlText.TextTokens;
            if (!textTokens.Any())
            {
                return false;
            }

            if (!allowWhitespace && textTokens.Count != 1)
            {
                return false;
            }

            if (textTokens.Any(t => !string.IsNullOrWhiteSpace(t.ToString())))
            {
                return false;
            }

            var lastTextToken = textTokens.Last();
            var firstTextToken = textTokens.First();

            return lastTextToken.Kind() == SyntaxKind.XmlTextLiteralNewLineToken
                && firstTextToken.LeadingTrivia.Count == 1
                && firstTextToken.LeadingTrivia.ElementAt(0).Kind() == SyntaxKind.DocumentationCommentExteriorTrivia
                && firstTextToken.LeadingTrivia.ElementAt(0).ToString() == ExteriorTriviaText
                && lastTextToken.TrailingTrivia.Count == 0;
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

            if (!(documentationComment.Content.LastOrDefault() is XmlTextSyntax xmlText))
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
}
