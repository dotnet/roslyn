// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.RemoveUnusedUsings
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnnecessaryImports), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.AddMissingReference)]
    internal class RemoveUnnecessaryUsingsCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CSharpRemoveUnnecessaryImportsDiagnosticAnalyzer.DiagnosticFixableId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var span = context.Span;
            var cancellationToken = context.CancellationToken;

            var service = document.GetLanguageService<IRemoveUnnecessaryImportsService>();
            var newDocument = await service.RemoveUnnecessaryImportsAsync(document, cancellationToken).ConfigureAwait(false);
            if (newDocument == document || newDocument == null)
            {
                return;
            }

            context.RegisterCodeFix(
                new MyCodeAction(
                    CSharpFeaturesResources.RemoveUnnecessaryUsings,
                    (c) => Task.FromResult(newDocument)),
                context.Diagnostics);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(title, createChangedDocument)
            {
            }
        }
    }
}
