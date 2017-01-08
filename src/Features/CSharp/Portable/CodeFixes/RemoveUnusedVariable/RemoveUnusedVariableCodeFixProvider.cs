// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveUnusedVar
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnusedVariable), Shared]
    internal partial class RemoveUnusedVariableCodeFixProvider : CodeFixProvider
    {
        private const string CS0168 = nameof(CS0168);
        private const string CS0219 = nameof(CS0219);

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CS0168, CS0219); }
        }

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);

            return SpecializedTasks.EmptyTask;
        }

        private async Task<Document> FixAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(diagnostic.Location.SourceSpan.Start);
            var ancestor = token.GetAncestor<LocalDeclarationStatementSyntax>();
            root = root.RemoveNode(ancestor, SyntaxRemoveOptions.KeepNoTrivia);

            return document.WithSyntaxRoot(root);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(CSharpFeaturesResources.Remove_Unused_Variable, createChangedDocument, CSharpFeaturesResources.Remove_Unused_Variable)
            {
            }
        }
    }
}
