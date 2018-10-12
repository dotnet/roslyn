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
using Microsoft.CodeAnalysis.Editing;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.UseAsyncForEach
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseAsyncForEach), Shared]
    internal sealed class CSharpUseAsyncForEachCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        private const string ForEachWrongAsync = "CS8414";

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(ForEachWrongAsync);
  // 1         ERR_AsyncForEachMissingMemberWrongAsync = 8415,
//ERR_NoConvToIAsyncDispWrongAsync = 8417,
//   1         ERR_NoConvToIDispWrongAsync = 8418,

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);

            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var root = editor.OriginalRoot;
            foreach (var diagnostic in diagnostics)
            {
                var expression = root.FindNode(diagnostic.Location.SourceSpan);
                var loop = expression.FirstAncestorOrSelf<ForEachStatementSyntax>();
                editor.ReplaceNode(loop, loop.WithAwaitKeyword(SyntaxFactory.Token(SyntaxKind.AwaitKeyword)));
            }

            return Task.CompletedTask;
        }

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(FeaturesResources.Use_asynchronous_foreach, createChangedDocument, FeaturesResources.Use_asynchronous_foreach)
            {
            }
        }
    }
}
