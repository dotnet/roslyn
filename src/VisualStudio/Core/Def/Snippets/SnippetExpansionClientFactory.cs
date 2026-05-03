// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Snippets;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets;

[ExportWorkspaceService(typeof(ISnippetExpansionClientFactory))]
[Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class SnippetExpansionClientFactory(
    IThreadingContext threadingContext,
    SignatureHelpControllerProvider signatureHelpControllerProvider,
    IEditorCommandHandlerServiceFactory editorCommandHandlerServiceFactory,
    IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
    [ImportMany] IEnumerable<Lazy<ArgumentProvider, OrderableLanguageMetadata>> argumentProviders,
    EditorOptionsService editorOptionsService)
    : ISnippetExpansionClientFactory
{
    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly SignatureHelpControllerProvider _signatureHelpControllerProvider = signatureHelpControllerProvider;
    private readonly IEditorCommandHandlerServiceFactory _editorCommandHandlerServiceFactory = editorCommandHandlerServiceFactory;
    private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService = editorAdaptersFactoryService;
    private readonly ImmutableArray<Lazy<ArgumentProvider, OrderableLanguageMetadata>> _argumentProviders = [.. argumentProviders];
    private readonly EditorOptionsService _editorOptionsService = editorOptionsService;

    public SnippetExpansionClient? TryGetSnippetExpansionClient(ITextView textView)
    {
        Contract.ThrowIfFalse(_threadingContext.JoinableTaskContext.IsOnMainThread);

        _ = textView.Properties.TryGetProperty(typeof(SnippetExpansionClient), out SnippetExpansionClient? expansionClient);
        return expansionClient;
    }

    public SnippetExpansionClient GetOrCreateSnippetExpansionClient(Document document, ITextView textView, ITextBuffer subjectBuffer)
    {
        Contract.ThrowIfFalse(_threadingContext.JoinableTaskContext.IsOnMainThread);

        if (!textView.Properties.TryGetProperty(typeof(SnippetExpansionClient), out SnippetExpansionClient? expansionClient))
        {
            expansionClient = CreateSnippetExpansionClient(document, textView, subjectBuffer);
            textView.Properties.AddProperty(typeof(SnippetExpansionClient), expansionClient);
        }

        return expansionClient!;
    }

    protected virtual SnippetExpansionClient CreateSnippetExpansionClient(Document document, ITextView textView, ITextBuffer subjectBuffer)
    {
        return new SnippetExpansionClient(
            _threadingContext,
            document.GetRequiredLanguageService<ISnippetExpansionLanguageHelper>(),
            textView,
            subjectBuffer,
            _signatureHelpControllerProvider,
            _editorCommandHandlerServiceFactory,
            _editorAdaptersFactoryService,
            _argumentProviders,
            _editorOptionsService);
    }
}
