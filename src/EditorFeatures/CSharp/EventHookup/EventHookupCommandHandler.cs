// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EventHookup
{
    /// <summary>
    /// Ignores commands until '=' is pressed, at which point we determine if the '=' is part of a
    /// "+=" that is used to attach an event handler to an event. Once we determine that it is a
    /// "+=" attaching an event handler to an event, we show a UI that tells the user they can hit
    /// tab to generate a handler method. 
    /// 
    /// Once we receive the '=' we watch all actions within the buffer. Anything (including use of 
    /// directional arrows) other than a typed space removes the UI or cancels any background
    /// computation.
    /// 
    /// The determination of whether the "+=" is being used to attach an event handler to an event
    /// can be costly, so it operates on a background thread. After the '=' of a "+=" is typed,
    /// only a tab will cause the UI thread to block while it determines whether we should 
    /// intercept the tab and generate an event handler or just let the tab through to other 
    /// handlers.
    /// 
    /// Because we are explicitly asking the user to tab, so we should handle the tab command before
    /// Automatic Completion.
    /// </summary>
    [ExportCommandHandler(PredefinedCommandHandlerNames.EventHookup, ContentTypeNames.CSharpContentType)]
    [Order(Before = PredefinedCommandHandlerNames.AutomaticCompletion)]
    internal partial class EventHookupCommandHandler : ForegroundThreadAffinitizedObject
    {
        private readonly IInlineRenameService _inlineRenameService;
        private readonly AggregateAsynchronousOperationListener _asyncListener;
        private readonly Microsoft.CodeAnalysis.Editor.Host.IWaitIndicator _waitIndicator;

        internal readonly EventHookupSessionManager EventHookupSessionManager;

        // This can be null! It is only needed for VS and is thus only exported for VS scenarios.
        private readonly IHACK_EventHookupDismissalOnBufferChangePreventerService _prematureDismissalPreventer;

        // For testing purposes only! Will always be null except in certain tests.
        internal Mutex TESTSessionHookupMutex;

        [ImportingConstructor]
        public EventHookupCommandHandler(
            IInlineRenameService inlineRenameService,
            Microsoft.CodeAnalysis.Editor.Host.IWaitIndicator waitIndicator,
            IQuickInfoBroker quickInfoBroker,
            [Import(AllowDefault = true)] IHACK_EventHookupDismissalOnBufferChangePreventerService prematureDismissalPreventer,
            [ImportMany] IEnumerable<Lazy<IAsynchronousOperationListener, FeatureMetadata>> asyncListeners)
        {
            _inlineRenameService = inlineRenameService;
            _waitIndicator = waitIndicator;
            _prematureDismissalPreventer = prematureDismissalPreventer;
            _asyncListener = new AggregateAsynchronousOperationListener(asyncListeners, FeatureAttribute.EventHookup);

            this.EventHookupSessionManager = new EventHookupSessionManager(prematureDismissalPreventer, quickInfoBroker);
        }
    }
}
