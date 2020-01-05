// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editor.Xaml.Diagnostics;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Xaml;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Xaml.CodeFixes.RemoveUnusedUsings
{
    [ExportCodeFixProvider(StringConstants.XamlLanguageName, Name = PredefinedCodeFixProviderNames.RemoveUnnecessaryImports), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.AddMissingReference)]
    internal class RemoveUnnecessaryUsingsCodeFixProvider : CodeFixProvider
    {
        [ImportingConstructor]
        public RemoveUnnecessaryUsingsCodeFixProvider()
        {
        }

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(XamlDiagnosticIds.UnnecessaryNamespacesId); }
        }

        public override FixAllProvider GetFixAllProvider()
        {
            // Fix All is not supported by this code fix, because the action already applies to one document at a time
            return null;
        }

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                new MyCodeAction(
                    c => RemoveUnnecessaryImportsAsync(context.Document, c)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        private Task<Document> RemoveUnnecessaryImportsAsync(
            Document document, CancellationToken cancellationToken)
        {
            var service = document.GetLanguageService<IRemoveUnnecessaryImportsService>();
            return service.RemoveUnnecessaryImportsAsync(document, cancellationToken);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(Resources.RemoveUnnecessaryNamespaces, createChangedDocument)
            {
            }
        }
    }
}
