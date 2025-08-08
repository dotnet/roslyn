// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Windows;
using Microsoft.CodeAnalysis.Editor.QuickInfo;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.QuickInfo.Presentation;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.QuickInfo;

[Export(typeof(IViewElementFactory))]
[Name("OnTheFlyDocsElement converter")]
[TypeConversion(from: typeof(QuickInfoOnTheFlyDocsElement), to: typeof(UIElement))]
[Order(Before = "Default")]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class OnTheFlyDocsViewFactory(
    IViewElementFactoryService factoryService,
    IAsynchronousOperationListenerProvider listenerProvider,
    IAsyncQuickInfoBroker asyncQuickInfoBroker,
    IThreadingContext threadingContext,
    SVsServiceProvider serviceProvider) : IViewElementFactory
{
    private readonly IViewElementFactoryService _factoryService = factoryService;
    private readonly IAsynchronousOperationListenerProvider _listenerProvider = listenerProvider;
    private readonly IAsyncQuickInfoBroker _asyncQuickInfoBroker = asyncQuickInfoBroker;
    private readonly IThreadingContext _threadingContext = threadingContext;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public TView? CreateViewElement<TView>(ITextView textView, object model) where TView : class
    {
        try
        {
            return CreateViewElementWorker<TView>(textView, model);
        }
        catch (Exception e) when (FatalError.ReportAndCatch(e))
        {
            return null;
        }
    }

    private TView? CreateViewElementWorker<TView>(ITextView textView, object model) where TView : class
    {
        if (typeof(TView) != typeof(UIElement))
            throw new InvalidOperationException("TView must be UIElement");

        var onTheFlyDocsElement = (QuickInfoOnTheFlyDocsElement)model;

        Logger.Log(FunctionId.Copilot_On_The_Fly_Docs_Showed_Link, logLevel: LogLevel.Information);

        var quickInfoSession = _asyncQuickInfoBroker.GetSession(textView) ?? throw new InvalidOperationException("QuickInfoSession is null");
        OnTheFlyDocsLogger.LogShowedOnTheFlyDocsLink();

        if (onTheFlyDocsElement.Info.HasComments)
        {
            OnTheFlyDocsLogger.LogShowedOnTheFlyDocsLinkWithDocComments();
        }

        return new OnTheFlyDocsView(textView, _factoryService, _listenerProvider, quickInfoSession, _threadingContext, onTheFlyDocsElement, _serviceProvider) as TView;
    }
}
