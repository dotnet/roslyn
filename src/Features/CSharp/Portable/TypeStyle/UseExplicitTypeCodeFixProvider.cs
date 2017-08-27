// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.TypeStyle
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseExplicitType), Shared]
    internal class UseExplicitTypeCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(IDEDiagnosticIds.UseExplicitTypeDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);

            return SpecializedTasks.EmptyTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, 
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var root = editor.OriginalRoot;

            foreach (var diagnostic in diagnostics)
            {
                var node = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
                await HandleDeclarationAsync(document, editor, node, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task HandleDeclarationAsync(
            Document document, SyntaxEditor editor, 
            SyntaxNode node, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var declarationContext = node.Parent;

            TypeSyntax typeSyntax = null;
            if (declarationContext is VariableDeclarationSyntax varDecl)
            {
                typeSyntax = varDecl.Type;
            }
            else if (declarationContext is ForEachStatementSyntax forEach)
            {
                typeSyntax = forEach.Type;
            }
            else if (declarationContext is DeclarationExpressionSyntax declarationExpression)
            {
                typeSyntax = declarationExpression.Type;
            }
            else
            {
                Contract.Fail($"unhandled kind {declarationContext.Kind().ToString()}");
            }

            var typeSymbol = semanticModel.GetTypeInfo(typeSyntax).ConvertedType;

            var typeName = typeSymbol.GenerateTypeSyntax()
                                     .WithLeadingTrivia(node.GetLeadingTrivia())
                                     .WithTrailingTrivia(node.GetTrailingTrivia());

            Debug.Assert(!typeName.ContainsDiagnostics, "Explicit type replacement likely introduced an error in code");

            editor.ReplaceNode(node, typeName);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(CSharpFeaturesResources.Use_explicit_type_instead_of_var,
                     createChangedDocument,
                     CSharpFeaturesResources.Use_explicit_type_instead_of_var)
            {
            }
        }
    }
}
