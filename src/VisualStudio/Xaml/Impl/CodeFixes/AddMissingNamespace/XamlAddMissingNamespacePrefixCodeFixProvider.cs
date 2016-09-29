// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editor.Xaml.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Xaml.Features;

namespace Microsoft.CodeAnalysis.Editor.Xaml.CodeFixes.AddMissingNamespace
{
    [ExportCodeFixProvider(StringConstants.XamlLanguageName, Name = PredefinedCodeFixProviderNames.AddUsingOrImport), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.AddMissingReference)]
    internal class XamlAddMissingNamespacePrefixCodeFixProvider : CodeFixProvider
    {
        private readonly IXamlAddMissingNamespaceService _missingNamespaceService;

        [ImportingConstructor]
        public XamlAddMissingNamespacePrefixCodeFixProvider(IXamlAddMissingNamespaceService missingNamespaceService)
        {
            _missingNamespaceService = missingNamespaceService;
        }

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(XamlDiagnosticIds.MissingNamespaceId); }
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var changeAction = await _missingNamespaceService.CreateMissingNamespaceFixAsync(context.Document, context.Span, context.CancellationToken).ConfigureAwait(false);
            if (changeAction == null)
            {
                return;
            }

            context.RegisterCodeFix(
                changeAction,
                context.Diagnostics);
        }
    }
}
