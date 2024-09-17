// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Windows;
using Microsoft.CodeAnalysis.Editor.QuickInfo;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.QuickInfo.Presentation;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.QuickInfo;

[Export(typeof(IViewElementFactory))]
[Name("OnTheFlyDocsElement converter")]
[TypeConversion(from: typeof(QuickInfoOnTheFlyDocsElement), to: typeof(UIElement))]
[Order(Before = "Default")]
internal sealed class OnTheFlyDocsViewFactory : IViewElementFactory
{
    private readonly IViewElementFactoryService _factoryService;
    private readonly IAsynchronousOperationListenerProvider _listenerProvider;
    private readonly IAsyncQuickInfoBroker _asyncQuickInfoBroker;
    private readonly IThreadingContext _threadingContext;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public OnTheFlyDocsViewFactory(IViewElementFactoryService factoryService, IAsynchronousOperationListenerProvider listenerProvider, IAsyncQuickInfoBroker asyncQuickInfoBroker, IThreadingContext threadingContext)
    {
        _factoryService = factoryService;
        _listenerProvider = listenerProvider;
        _asyncQuickInfoBroker = asyncQuickInfoBroker;
        _threadingContext = threadingContext;
    }

    public TView? CreateViewElement<TView>(ITextView textView, object model) where TView : class
    {
        if (typeof(TView) != typeof(UIElement))
        {
            throw new InvalidOperationException("TView must be UIElement");
        }

        var onTheFlyDocsElement = (QuickInfoOnTheFlyDocsElement)model;

        Logger.Log(FunctionId.Copilot_On_The_Fly_Docs_Showed_Link, KeyValueLogMessage.Create(m =>
        {
            m["SymbolHeaderText"] = onTheFlyDocsElement.Info.SymbolSignature;
        }, LogLevel.Information));

        var quickInfoSession = _asyncQuickInfoBroker.GetSession(textView);

        if (quickInfoSession is null)
        {
            throw new InvalidOperationException("QuickInfoSession is null");
        }

        OnTheFlyDocsLogger.LogShowedOnTheFlyDocsLink();

        if (onTheFlyDocsElement.Info.HasComments)
        {
            OnTheFlyDocsLogger.LogShowedOnTheFlyDocsLinkWithDocComments();
        }

        return new OnTheFlyDocsView(textView, _factoryService, _listenerProvider, quickInfoSession, _threadingContext, onTheFlyDocsElement) as TView;
    }
}
