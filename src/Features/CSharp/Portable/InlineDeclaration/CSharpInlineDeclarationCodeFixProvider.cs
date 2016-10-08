// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.FixAllOccurrences;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle.TypeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.InlineDeclaration
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpInlineDeclarationCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.InlineDeclarationDiagnosticId);

        public override FixAllProvider GetFixAllProvider() => new InlineDeclarationFixAllProvider(this);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);
            return SpecializedTasks.EmptyTask;
        }

        private Task<Document> FixAsync(
            Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            return FixAllAsync(document, ImmutableArray.Create(diagnostic), cancellationToken);
        }

        private async Task<Document> FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace);
            var options = document.Project.Solution.Workspace.Options;

            var useVarWhenDeclaringLocals = options.GetOption(CSharpCodeStyleOptions.UseVarWhenDeclaringLocals);
            var useImplicitTypeForIntrinsicTypes = options.GetOption(CSharpCodeStyleOptions.UseImplicitTypeForIntrinsicTypes).Value;

            foreach (var diagnostic in diagnostics)
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddEdits(root, editor, diagnostic, useVarWhenDeclaringLocals, useImplicitTypeForIntrinsicTypes, cancellationToken);
            }

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }

        private void AddEdits(
            SyntaxNode root, SyntaxEditor editor, Diagnostic diagnostic, 
            bool useVarWhenDeclaringLocals, bool useImplicitTypeForIntrinsicTypes, 
            CancellationToken cancellationToken)
        {
            var declaratorLocation = diagnostic.AdditionalLocations[0];
            var identifierLocation = diagnostic.AdditionalLocations[1];

            var declarator = (VariableDeclaratorSyntax)declaratorLocation.FindNode(cancellationToken);
            var identifier = (IdentifierNameSyntax)identifierLocation.FindNode(cancellationToken);

            var declaration = (VariableDeclarationSyntax)declarator.Parent;
            if (declaration.Variables.Count == 1)
            {
                // Remove the entire declaration statement.
                editor.RemoveNode(declaration.Parent);
            }
            else
            {
                // Otherwise, just remove the single declarator.
                editor.RemoveNode(declarator);
            }

            var type = this.GetDeclarationType(declaration.Type, useVarWhenDeclaringLocals, useImplicitTypeForIntrinsicTypes)
                           .WithoutTrivia()
                           .WithAdditionalAnnotations(Formatter.Annotation);

            var declarationExpression = SyntaxFactory.DeclarationExpression(
                SyntaxFactory.TypedVariableComponent(
                    type, SyntaxFactory.SingleVariableDesignation(identifier.Identifier)));

            editor.ReplaceNode(identifier, declarationExpression);
        }

        private TypeSyntax GetDeclarationType(
            TypeSyntax type, bool useVarWhenDeclaringLocals, bool useImplicitTypeForIntrinsicTypes)
        {
            if (useVarWhenDeclaringLocals)
            {
                if (useImplicitTypeForIntrinsicTypes ||
                    !TypeStyleHelper.IsPredefinedType(type))
                {
                    return SyntaxFactory.IdentifierName("var");
                }
            }

            return type;
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) 
                : base(FeaturesResources.Inline_variable_declaration, createChangedDocument)
            {
            }
        }

        private class InlineDeclarationFixAllProvider : DocumentBasedFixAllProvider
        {
            private readonly CSharpInlineDeclarationCodeFixProvider _provider;

            public InlineDeclarationFixAllProvider(CSharpInlineDeclarationCodeFixProvider provider)
            {
                _provider = provider;
            }

            protected override Task<Document> FixDocumentAsync(
                Document document, 
                ImmutableArray<Diagnostic> diagnostics, 
                CancellationToken cancellationToken)
            {
                return _provider.FixAllAsync(document, diagnostics, cancellationToken);
            }
        }
    }
}