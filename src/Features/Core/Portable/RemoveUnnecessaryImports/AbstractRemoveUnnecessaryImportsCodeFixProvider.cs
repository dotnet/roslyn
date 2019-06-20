// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryImports
{
    internal abstract class AbstractRemoveUnnecessaryImportsCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(AbstractRemoveUnnecessaryImportsDiagnosticAnalyzer.DiagnosticFixableId);

        public sealed override FixAllProvider GetFixAllProvider()
            => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(
                    GetTitle(),
                    c => RemoveUnnecessaryImportsAsync(context.Document, c)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        protected abstract string GetTitle();

        private Task<Document> RemoveUnnecessaryImportsAsync(
            Document document, CancellationToken cancellationToken)
        {
            var service = document.GetLanguageService<IRemoveUnnecessaryImportsService>();
            return service.RemoveUnnecessaryImportsAsync(document, cancellationToken);
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
