﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            var title = GetTitle();
            context.RegisterCodeFix(
                CodeAction.Create(
                    title,
                    c => RemoveUnnecessaryImportsAsync(context.Document, c),
                    title),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        protected abstract string GetTitle();

        private static Task<Document> RemoveUnnecessaryImportsAsync(
            Document document, CancellationToken cancellationToken)
        {
            var service = document.GetRequiredLanguageService<IRemoveUnnecessaryImportsService>();
            return service.RemoveUnnecessaryImportsAsync(document, cancellationToken);
        }
    }
}
