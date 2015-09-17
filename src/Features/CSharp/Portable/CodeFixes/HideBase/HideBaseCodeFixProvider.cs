// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeFixes;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Shared.Extensions;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.HideBase
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddNew), Shared]
    internal partial class HideBaseCodeFixProvider : CodeFixProvider
    {
        internal const string CS0108 = "CS0108"; // 'SomeClass.SomeMember' hides inherited member 'SomeClass.SomeMember'. Use the new keyword if hiding was intended.

        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(CS0108);
            }
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var token = root.FindToken(diagnosticSpan.Start);
            SyntaxNode originalNode = token.GetAncestor<PropertyDeclarationSyntax>();
            
            if (originalNode == null)
            {
                originalNode = token.GetAncestor<MethodDeclarationSyntax>();
            }

            if(originalNode == null)
            {
                originalNode = token.GetAncestor<FieldDeclarationSyntax>();
            }

            if(originalNode == null)
            {
                return;
            }

            var newNode = GetNewNode(context.Document, originalNode, context.CancellationToken);

            if (newNode == null)
            {
                return;
            }

            context.RegisterCodeFix(new AddNewKeywordAction(context.Document, originalNode, newNode), context.Diagnostics);
        }

        private SyntaxNode GetNewNode(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            SyntaxNode newNode = null;

            var propertyStatement = node as PropertyDeclarationSyntax;
            if (propertyStatement != null)
            {
                newNode = propertyStatement.AddModifiers(SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.NewKeyword, SyntaxTriviaList.Create(SyntaxFactory.Whitespace(" ")))) as SyntaxNode;
            }

            var methodStatement = node as MethodDeclarationSyntax;
            if (methodStatement != null)
            {
                newNode = methodStatement.AddModifiers(SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.NewKeyword, SyntaxTriviaList.Create(SyntaxFactory.Whitespace(" "))));
            }

            var fieldDeclaration = node as FieldDeclarationSyntax;
            if (fieldDeclaration != null)
            {
                newNode = fieldDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.NewKeyword, SyntaxTriviaList.Create(SyntaxFactory.Whitespace(" "))));
            }

            var cleanupService = document.GetLanguageService<ICodeCleanerService>();

            if (cleanupService != null && newNode != null)
            {
                newNode = cleanupService.Cleanup(newNode, new[] { newNode.Span }, document.Project.Solution.Workspace, cleanupService.GetDefaultProviders(), cancellationToken);
            }

            return newNode;
        }
    }
}
