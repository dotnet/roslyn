// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Implementation.DocumentationComments;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.CSharp.DocumentationComments
{
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Name(PredefinedCommandHandlerNames.DocumentationComments)]
    [Order(After = PredefinedCommandHandlerNames.Rename)]
    [Order(After = PredefinedCompletionNames.CompletionCommandHandler)]
    internal class DocumentationCommentCommandHandler
        : AbstractDocumentationCommentCommandHandler<DocumentationCommentTriviaSyntax, MemberDeclarationSyntax>
    {
        [ImportingConstructor]
        public DocumentationCommentCommandHandler(
            IWaitIndicator waitIndicator,
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService)
            : base(waitIndicator, undoHistoryRegistry, editorOperationsFactoryService)
        {
        }

        protected override string ExteriorTriviaText => "///";

        protected override MemberDeclarationSyntax GetContainingMember(
            SyntaxTree syntaxTree, int position, CancellationToken cancellationToken)
        {
            return syntaxTree.GetRoot(cancellationToken).FindToken(position).GetAncestor<MemberDeclarationSyntax>();
        }

        protected override bool SupportsDocumentationComments(MemberDeclarationSyntax member)
        {
            if (member == null)
            {
                return false;
            }

            switch (member.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                case SyntaxKind.StructDeclaration:
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
        {
            return member.GetFirstToken().LeadingTrivia.Any(SyntaxKind.SingleLineDocumentationCommentTrivia, SyntaxKind.MultiLineDocumentationCommentTrivia);
        }

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
        {
            return true;
        }

        protected override List<string> GetDocumentationCommentStubLines(MemberDeclarationSyntax member)
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

            if (member.IsKind(SyntaxKind.MethodDeclaration) ||
                member.IsKind(SyntaxKind.IndexerDeclaration) ||
                member.IsKind(SyntaxKind.DelegateDeclaration) ||
                member.IsKind(SyntaxKind.OperatorDeclaration))
            {
                var returnType = member.GetMemberType();
                if (returnType != null &&
                    !(returnType.IsKind(SyntaxKind.PredefinedType) && ((PredefinedTypeSyntax)returnType).Keyword.IsKindOrHasMatchingText(SyntaxKind.VoidKeyword)))
                {
                    list.Add("/// <returns></returns>");
                }
            }

            return list;
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
        {
            return token.RawKind == (int)SyntaxKind.XmlTextLiteralNewLineToken;
        }

        protected override bool IsEndOfLineTrivia(SyntaxTrivia trivia)
        {
            return trivia.RawKind == (int)SyntaxKind.EndOfLineTrivia;
        }

        protected override bool IsSingleExteriorTrivia(DocumentationCommentTriviaSyntax documentationComment, bool allowWhitespace = false)
        {
            if (documentationComment == null)
            {
                return false;
            }

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

        private IList<SyntaxToken> GetTextTokensFollowingExteriorTrivia(XmlTextSyntax xmlText)
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

        protected override bool EndsWithSingleExteriorTrivia(DocumentationCommentTriviaSyntax documentationComment)
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

        protected override bool IsMultilineDocComment(DocumentationCommentTriviaSyntax documentationComment)
        {
            return documentationComment.IsMultilineDocComment();
        }

        protected override bool AddIndent
        {
            get { return true; }
        }

        internal override bool HasSkippedTrailingTrivia(SyntaxToken token) => token.TrailingTrivia.Any(t => t.Kind() == SyntaxKind.SkippedTokensTrivia);
    }
}
