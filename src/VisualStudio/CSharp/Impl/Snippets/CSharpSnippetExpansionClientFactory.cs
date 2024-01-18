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
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.Snippets;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Snippets;

[ExportLanguageService(typeof(ISnippetExpansionClientFactory), LanguageNames.CSharp)]
[Shared]
internal sealed class CSharpSnippetExpansionClientFactory : AbstractSnippetExpansionClientFactory
{
    private readonly IThreadingContext _threadingContext;
    private readonly SignatureHelpControllerProvider _signatureHelpControllerProvider;
    private readonly IEditorCommandHandlerServiceFactory _editorCommandHandlerServiceFactory;
    private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
    private readonly ImmutableArray<Lazy<ArgumentProvider, OrderableLanguageMetadata>> _argumentProviders;
    private readonly EditorOptionsService _editorOptionsService;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpSnippetExpansionClientFactory(
        IThreadingContext threadingContext,
        SignatureHelpControllerProvider signatureHelpControllerProvider,
        IEditorCommandHandlerServiceFactory editorCommandHandlerServiceFactory,
        IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
        [ImportMany] IEnumerable<Lazy<ArgumentProvider, OrderableLanguageMetadata>> argumentProviders,
        EditorOptionsService editorOptionsService)
    {
        _threadingContext = threadingContext;
        _signatureHelpControllerProvider = signatureHelpControllerProvider;
        _editorCommandHandlerServiceFactory = editorCommandHandlerServiceFactory;
        _editorAdaptersFactoryService = editorAdaptersFactoryService;
        _argumentProviders = argumentProviders.ToImmutableArray();
        _editorOptionsService = editorOptionsService;
    }

    protected override AbstractSnippetExpansionClient CreateSnippetExpansionClient(ITextView textView, ITextBuffer subjectBuffer)
    {
        return new SnippetExpansionClient(
            _threadingContext,
            Guids.CSharpLanguageServiceId,
            textView,
            subjectBuffer,
            _signatureHelpControllerProvider,
            _editorCommandHandlerServiceFactory,
            _editorAdaptersFactoryService,
            _argumentProviders,
            _editorOptionsService);
    }
}
