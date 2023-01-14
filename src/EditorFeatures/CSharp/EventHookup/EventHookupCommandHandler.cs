// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Commanding;
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
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Name(PredefinedCommandHandlerNames.EventHookup)]
    [Order(Before = PredefinedCommandHandlerNames.AutomaticCompletion)]
    internal partial class EventHookupCommandHandler
    {
        private readonly IThreadingContext _threadingContext;
        private readonly IInlineRenameService _inlineRenameService;
        private readonly IAsynchronousOperationListener _asyncListener;
        private readonly IGlobalOptionService _globalOptions;

        internal readonly EventHookupSessionManager EventHookupSessionManager;

        // For testing purposes only! Will always be null except in certain tests.
        internal Mutex TESTSessionHookupMutex;

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public EventHookupCommandHandler(
            IThreadingContext threadingContext,
            IInlineRenameService inlineRenameService,
            EventHookupSessionManager eventHookupSessionManager,
            IGlobalOptionService globalOptions,
            IAsynchronousOperationListenerProvider listenerProvider)
        {
            _threadingContext = threadingContext;
            _inlineRenameService = inlineRenameService;
            _asyncListener = listenerProvider.GetListener(FeatureAttribute.EventHookup);
            _globalOptions = globalOptions;

            this.EventHookupSessionManager = eventHookupSessionManager;
        }
    }
}
