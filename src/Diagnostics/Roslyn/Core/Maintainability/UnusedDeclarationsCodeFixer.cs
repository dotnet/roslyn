// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Roslyn.Diagnostics.Analyzers
{
    [ExportCodeFixProvider(LanguageNames.CSharp, LanguageNames.VisualBasic, Name = nameof(UnusedDeclarationsCodeFixProvider)), Shared]
    internal class UnusedDeclarationsCodeFixProvider : CodeFixProvider
    {
        private static readonly Task s_done = Task.Run(() => { });

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(RoslynDiagnosticIds.DeadCodeTriggerRuleId); }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var document = context.Document;
            context.RegisterCodeFix(new MyCodeAction(RoslynDiagnosticsResources.UnusedDeclarationsCodeFixTitle, async c =>
            {
                var text = await document.GetTextAsync(c).ConfigureAwait(false);
                return document.WithText(text.Replace(context.Span, string.Empty));
            }, RoslynDiagnosticIds.DeadCodeTriggerRuleId), context.Diagnostics);

            return s_done;
        }

        private class MyCodeAction : DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> changedDocument, string key) :
                base(title, changedDocument, key)
            {
            }
        }
    }
}
