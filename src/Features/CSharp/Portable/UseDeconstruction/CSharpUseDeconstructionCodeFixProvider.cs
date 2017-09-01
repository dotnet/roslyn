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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseDeconstruction
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal class CSharpUseDeconstructionCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        private readonly CSharpUseDeconstructionDiagnosticAnalyzer s_analyzer = new CSharpUseDeconstructionDiagnosticAnalyzer();

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseDeconstructionDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, context.Diagnostics[0], c)),
                context.Diagnostics);

            return SpecializedTasks.EmptyTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var nodesToProcess = new Queue<SyntaxNode>(diagnostics.Select(d => d.Location.FindToken(cancellationToken).Parent));

            var originalRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var currentRoot = originalRoot.TrackNodes(nodesToProcess);
            var currentDocument = document.WithSyntaxRoot(currentRoot);
            var currentSemanticModel = await currentDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            while (nodesToProcess.Count > 0)
            {
                var currentNode = currentRoot.GetCurrentNode(nodesToProcess.Dequeue());
                if (currentNode is VariableDeclaratorSyntax variableDeclarator)
                {
                    if (s_analyzer.TryAnalyzeVariableDeclaration(
                            currentSemanticModel, (VariableDeclarationSyntax)variableDeclarator.Parent,
                            out var memberAccessExpressions, cancellationToken))
                    {
                        var newRoot = currentRoot.ReplaceNodes(
                            memberAccessExpressions,
                            (node, _) =>
                            {
                                var memberAccess = (MemberAccessExpressionSyntax)node;
                                return memberAccess.Name.WithTriviaFrom(memberAccess);
                            });

                        currentRoot = newRoot;
                        currentDocument = currentDocument.WithSyntaxRoot(currentRoot);
                        currentSemanticModel = await currentDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            editor.ReplaceNode(originalRoot, currentRoot);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) 
                : base(FeaturesResources.Deconstruct_variable_declaration, createChangedDocument, FeaturesResources.Deconstruct_variable_declaration)
            {
            }
        }
    }
}
