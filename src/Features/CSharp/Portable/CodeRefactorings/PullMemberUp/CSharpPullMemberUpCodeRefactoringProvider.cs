// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp;
using Microsoft.CodeAnalysis.CodeRefactorings.PullMemberUp.Dialog;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.PullMemberUp
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(PredefinedCodeRefactoringProviderNames.PullMemberUp)), Shared]
    internal class CSharpPullMemberUpCodeRefactoringProvider : AbstractPullMemberUpRefactoringProvider
    {
        /// <summary>
        /// Test purpose only.
        /// </summary>
        internal CSharpPullMemberUpCodeRefactoringProvider(IPullMemberUpOptionsService service) : base(service)
        {
        }

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        internal CSharpPullMemberUpCodeRefactoringProvider() : base(null)
        {
        }

        protected override bool IsSelectionValid(TextSpan span, SyntaxNode selectedNode)
        {
            var identifier = GetIdentifier(selectedNode);
            if (identifier == default)
            {
                return false;
            }
            else if (identifier.FullSpan.Contains(span) && span.Contains(identifier.Span))
            {
                // Selection lies within the identifier's span
                return true;
            }
            else if (identifier.Span.Contains(span) && span.IsEmpty)
            {
                // Cursor stands on the identifier
                return true;
            }
            else
            {
                return false;
            }
        }

        protected async override Task<SyntaxNode> GetMatchedNodeAsync(Document document, TextSpan span)
        {
            var root = await document.GetSyntaxRootAsync().ConfigureAwait(false);
            // root.FindNode() will return the syntax node contains the parenthesis or equal sign in the following scenario,
            // void Bar[||]();
            // int i[||]= 0;
            // int j[||]=> 100;
            // int k[||]{set; }
            // but refactoring should be provide in for this cases, so move the cursor back
            // one step to get the syntax node.
            var token = root.FindToken(span.Start);
            var isSpecialCaseToken =
                token.IsKind(SyntaxKind.OpenParenToken) ||
                token.IsKind(SyntaxKind.OpenBraceToken) ||
                token.IsKind(SyntaxKind.EqualsGreaterThanToken) ||
                token.IsKind(SyntaxKind.EqualsToken);
            if (span.IsEmpty && isSpecialCaseToken)
            {
                // move the span one step back
                var relocatedSpan = new TextSpan(span.Start > 0 ? span.Start - 1 : 0, length: 0);
                // Symbol of this node will be checked later to make sure it is a member
                return root.FindNode(relocatedSpan);
            }
            else
            {
                return root.FindNode(span);
            }
        }

        private SyntaxToken GetIdentifier(SyntaxNode selectedNode)
        {
            switch (selectedNode)
            {
                case MemberDeclarationSyntax memberDeclarationSyntax:
                    // Nested type is checked in before this method is called.
                    return memberDeclarationSyntax.GetNameToken();
                case VariableDeclaratorSyntax variableDeclaratorSyntax:
                    // It handles multiple fields or events declared in one line
                    return variableDeclaratorSyntax.Identifier;
                default:
                    return default;
            }
        }
    }
}
