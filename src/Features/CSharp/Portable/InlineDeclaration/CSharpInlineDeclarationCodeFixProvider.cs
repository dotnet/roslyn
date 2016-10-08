// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle.TypeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.InlineDeclaration
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal partial class CSharpInlineDeclarationCodeFixProvider : CodeFixProvider
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

        private Task<Document> FixAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
            => FixAllAsync(document, ImmutableArray.Create(diagnostic), cancellationToken);

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
                await AddEditsAsync(document, editor, diagnostic, 
                    useVarWhenDeclaringLocals, useImplicitTypeForIntrinsicTypes, 
                    cancellationToken).ConfigureAwait(false);
            }

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
        }

        private async Task AddEditsAsync(
            Document document, SyntaxEditor editor, Diagnostic diagnostic, 
            bool useVarWhenDeclaringLocals, bool useImplicitTypeForIntrinsicTypes, CancellationToken cancellationToken)
        {
            var declaratorLocation = diagnostic.AdditionalLocations[0];
            var identifierLocation = diagnostic.AdditionalLocations[1];
            var invocationOrCreationLocation = diagnostic.AdditionalLocations[2];

            var declarator = (VariableDeclaratorSyntax)declaratorLocation.FindNode(cancellationToken);
            var identifier = (IdentifierNameSyntax)identifierLocation.FindNode(cancellationToken);
            var invocationOrCreation = (ExpressionSyntax)invocationOrCreationLocation.FindNode(cancellationToken);

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

            var newType = this.GetDeclarationType(declaration.Type, useVarWhenDeclaringLocals, useImplicitTypeForIntrinsicTypes);

            var declarationExpression = GetDeclarationExpression(identifier, newType);

            var semanticsChanged = await SemanticsChangedAsync(
                document, declaration, invocationOrCreation, newType,
                identifier, declarationExpression, cancellationToken).ConfigureAwait(false);
            if (semanticsChanged)
            {
                // Switching to 'var' changed semantics.  Just use the original type the user wrote.
                declarationExpression = GetDeclarationExpression(identifier, declaration.Type);
            }

            editor.ReplaceNode(identifier, declarationExpression);
        }

        private static DeclarationExpressionSyntax GetDeclarationExpression(
            IdentifierNameSyntax identifier, TypeSyntax newType)
        {
            newType = newType.WithoutTrivia().WithAdditionalAnnotations(Formatter.Annotation);
            var declarationExpression = SyntaxFactory.DeclarationExpression(
                SyntaxFactory.TypedVariableComponent(
                    newType, SyntaxFactory.SingleVariableDesignation(identifier.Identifier)));
            return declarationExpression;
        }

        private async Task<bool> SemanticsChangedAsync(
            Document document,
            VariableDeclarationSyntax declaration,
            ExpressionSyntax invocationOrCreation,
            TypeSyntax newType,
            IdentifierNameSyntax identifier,
            DeclarationExpressionSyntax declarationExpression,
            CancellationToken cancellationToken)
        {
            if (!declaration.Type.IsVar &&
                newType.IsVar)
            {
                // Options want us to use 'var' if we can.  Make sure we didn't change
                // the semantics of teh call by doing this.

                // Find the symbol that the existing invocation points to.
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var previousSymbol = semanticModel.GetSymbolInfo(invocationOrCreation).Symbol;

                var annotation = new SyntaxAnnotation();
                var updatedInvocationOrCreation = invocationOrCreation.ReplaceNode(
                    identifier, declarationExpression).WithAdditionalAnnotations(annotation);

                // Note(cyrusn): "https://github.com/dotnet/roslyn/issues/14384" prevents us from just
                // speculatively binding the new expression.  So, instead, we fork things and see if
                // the new symbol we bind to is equivalent to the previous one.
                var newDocument = document.WithSyntaxRoot(
                    root.ReplaceNode(invocationOrCreation, updatedInvocationOrCreation));

                var newRoot = await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var newSemanticModel = await newDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

                updatedInvocationOrCreation = (ExpressionSyntax)newRoot.GetAnnotatedNodes(annotation).Single();

                var updatedSymbol = newSemanticModel.GetSymbolInfo(updatedInvocationOrCreation).Symbol;

                if (!SymbolEquivalenceComparer.Instance.Equals(previousSymbol, updatedSymbol))
                {
                    // We're pointing at a new symbol now.  Semantic have changed.
                    return true;
                }
            }

            return false;
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
    }
}