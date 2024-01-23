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
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpSnippetExpansionClientFactory(
    IThreadingContext threadingContext,
    SignatureHelpControllerProvider signatureHelpControllerProvider,
    IEditorCommandHandlerServiceFactory editorCommandHandlerServiceFactory,
    IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
    [ImportMany] IEnumerable<Lazy<ArgumentProvider, OrderableLanguageMetadata>> argumentProviders,
    EditorOptionsService editorOptionsService)
    : AbstractSnippetExpansionClientFactory(threadingContext)
{
    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly SignatureHelpControllerProvider _signatureHelpControllerProvider = signatureHelpControllerProvider;
    private readonly IEditorCommandHandlerServiceFactory _editorCommandHandlerServiceFactory = editorCommandHandlerServiceFactory;
    private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService = editorAdaptersFactoryService;
    private readonly ImmutableArray<Lazy<ArgumentProvider, OrderableLanguageMetadata>> _argumentProviders = argumentProviders.ToImmutableArray();
    private readonly EditorOptionsService _editorOptionsService = editorOptionsService;

    protected override AbstractSnippetExpansionClient CreateSnippetExpansionClient(ITextView textView, ITextBuffer subjectBuffer)
    {
        return new CSharpSnippetExpansionClient(
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
