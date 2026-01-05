// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.SmartRename;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

[Export(typeof(IWpfTextViewConnectionListener))]
[ContentType(ContentTypeNames.RoslynContentType)]
[ContentType(ContentTypeNames.XamlContentType)]
[TextViewRole(PredefinedTextViewRoles.Interactive)]
internal sealed class InlineRenameAdornmentProvider : IWpfTextViewConnectionListener
{
    private readonly InlineRenameService _renameService;
    private readonly IInlineRenameColorUpdater? _dashboardColorUpdater;
    private readonly IWpfThemeService? _themeingService;
    private readonly IGlobalOptionService _globalOptionService;
    private readonly IAsyncQuickInfoBroker _asyncQuickInfoBroker;
    private readonly IAsynchronousOperationListenerProvider _listenerProvider;
    private readonly IThreadingContext _threadingContext;

#pragma warning disable CS0618 // Editor team use Obsolete attribute to mark potential changing API
    private readonly Lazy<ISmartRenameSessionFactory>? _smartRenameSessionFactory;
#pragma warning restore CS0618

    public const string AdornmentLayerName = "RoslynRenameDashboard";

    [Export]
    [Name(AdornmentLayerName)]
    [Order(After = PredefinedAdornmentLayers.Outlining)]
    [Order(After = PredefinedAdornmentLayers.Text)]
    [Order(After = PredefinedAdornmentLayers.Selection)]
    [Order(After = PredefinedAdornmentLayers.Caret)]
    [Order(After = PredefinedAdornmentLayers.TextMarker)]
    [Order(After = PredefinedAdornmentLayers.CurrentLineHighlighter)]
    [Order(After = PredefinedAdornmentLayers.Squiggle)]
    internal readonly AdornmentLayerDefinition? AdornmentLayer;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public InlineRenameAdornmentProvider(
        InlineRenameService renameService,
        [Import(AllowDefault = true)] IInlineRenameColorUpdater? dashboardColorUpdater,
        [Import(AllowDefault = true)] IWpfThemeService? themeingService,
        IGlobalOptionService globalOptionService,
        IAsyncQuickInfoBroker asyncQuickInfoBroker,
        IAsynchronousOperationListenerProvider listenerProvider,
        IThreadingContext threadingContext,
#pragma warning disable CS0618 // Editor team use Obsolete attribute to mark potential changing API
        [Import(AllowDefault = true)] Lazy<ISmartRenameSessionFactory>? smartRenameSessionFactory)
#pragma warning restore CS0618
    {
        _renameService = renameService;
        _dashboardColorUpdater = dashboardColorUpdater;
        _themeingService = themeingService;
        _globalOptionService = globalOptionService;
        _asyncQuickInfoBroker = asyncQuickInfoBroker;
        _listenerProvider = listenerProvider;
        _smartRenameSessionFactory = smartRenameSessionFactory;
        _threadingContext = threadingContext;
    }

    public void SubjectBuffersConnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
    {
        // Create it for the view if we don't already have one
        textView.GetOrCreateAutoClosingProperty(v => new InlineRenameAdornmentManager(
            _renameService, _dashboardColorUpdater, v, _globalOptionService, _themeingService, _asyncQuickInfoBroker, _listenerProvider, _threadingContext, _smartRenameSessionFactory));
    }

    public void SubjectBuffersDisconnected(IWpfTextView textView, ConnectionReason reason, Collection<ITextBuffer> subjectBuffers)
    {
        // Do we still have any buffers alive?
        if (textView.BufferGraph.GetTextBuffers(b => b.ContentType.IsOfType(ContentTypeNames.RoslynContentType)).Any())
        {
            // Yep, some are still attached
            return;
        }

        if (textView.Properties.TryGetProperty(typeof(InlineRenameAdornmentManager), out InlineRenameAdornmentManager manager))
        {
            manager.Dispose();
            textView.Properties.RemoveProperty(typeof(InlineRenameAdornmentManager));
        }
    }
}
