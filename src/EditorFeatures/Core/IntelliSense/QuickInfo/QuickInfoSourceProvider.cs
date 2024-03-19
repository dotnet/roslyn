// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.InlineRename;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;

[ContentType(ContentTypeNames.RoslynContentType)]
[Export(typeof(IAsyncQuickInfoSourceProvider))]
[Name("RoslynQuickInfoProvider")]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal partial class QuickInfoSourceProvider(
    IThreadingContext threadingContext,
    IUIThreadOperationExecutor operationExecutor,
    IAsynchronousOperationListenerProvider listenerProvider,
    Lazy<IStreamingFindUsagesPresenter> streamingPresenter,
    EditorOptionsService editorOptionsService,
    IInlineRenameService inlineRenameService) : IAsyncQuickInfoSourceProvider
{
    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly IUIThreadOperationExecutor _operationExecutor = operationExecutor;
    private readonly Lazy<IStreamingFindUsagesPresenter> _streamingPresenter = streamingPresenter;
    private readonly IAsynchronousOperationListener _listener = listenerProvider.GetListener(FeatureAttribute.QuickInfo);
    private readonly EditorOptionsService _editorOptionsService = editorOptionsService;
    private readonly IInlineRenameService _inlineRenameService = inlineRenameService;

    public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer)
    {
        if (textBuffer.IsInLspEditorContext())
            return null;

        return new QuickInfoSource(
            textBuffer, _threadingContext, _operationExecutor, _listener, _streamingPresenter, _editorOptionsService, _inlineRenameService);
    }
}
