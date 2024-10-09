// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editor.Xaml.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Xaml;

namespace Microsoft.CodeAnalysis.Editor.Xaml.CodeFixes.RemoveUnusedUsings
{
    [ExportCodeFixProvider(StringConstants.XamlLanguageName, Name = PredefinedCodeFixProviderNames.RemoveUnnecessaryImports), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.AddMissingReference)]
    internal class RemoveUnnecessaryUsingsCodeFixProvider : CodeFixProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoveUnnecessaryUsingsCodeFixProvider()
        {
        }

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return [XamlDiagnosticIds.UnnecessaryNamespacesId]; }
        }

        public override FixAllProvider GetFixAllProvider()
        {
            // Fix All is not supported by this code fix, because the action already applies to one document at a time
            return null;
        }

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    Resources.RemoveUnnecessaryNamespaces,
                    c => RemoveUnnecessaryImportsAsync(context.Document, c),
                    nameof(Resources.RemoveUnnecessaryNamespaces)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        private static async Task<Document> RemoveUnnecessaryImportsAsync(
            Document document, CancellationToken cancellationToken)
        {
            var service = document.GetLanguageService<IRemoveUnnecessaryImportsService>();
            return await service.RemoveUnnecessaryImportsAsync(document, cancellationToken).ConfigureAwait(false);
        }
    }
}
