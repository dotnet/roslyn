// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.Shell;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices
{
    /// <summary>
    /// Monitors Visual Studio's UIContext for Solution building/opening/closing and notifies the GlobalOperationService.
    /// The intent is to suspend analysis of non-essential files for the duration of these solution operations.
    /// </summary>
    internal sealed class SolutionEventMonitor : IDisposable
    {
        private const string SolutionBuilding = "Solution Building";
        private const string SolutionOpening = "Solution Opening";
        private const string SolutionClosing = "Solution Closing";

        private readonly UIContext _solutionClosingContext = UIContext.FromUIContextGuid(VSConstants.UICONTEXT.SolutionClosing_guid);
        private readonly IGlobalOperationNotificationService _notificationService;
        private readonly Dictionary<string, IDisposable> _operations = new();

        public SolutionEventMonitor(IGlobalOperationNotificationService notificationService)
        {
            Contract.ThrowIfNull(notificationService);
            _notificationService = notificationService;

            RegisterEventHandler(KnownUIContexts.SolutionBuildingContext, SolutionBuildingContextChanged);
            RegisterEventHandler(KnownUIContexts.SolutionOpeningContext, SolutionOpeningContextChanged);
            RegisterEventHandler(_solutionClosingContext, SolutionClosingContextChanged);

            static void RegisterEventHandler(UIContext context, EventHandler<UIContextChangedEventArgs> handler)
            {
                // make sure we set initial state correctly. otherwise, we can get into a race where we might miss the very first events
                if (context.IsActive)
                    handler(sender: null, UIContextChangedEventArgs.From(activated: true));

                context.UIContextChanged += handler;
            }
        }

        public void Dispose()
        {
            foreach (var globalOperation in _operations.Values)
                globalOperation.Dispose();

            _operations.Clear();

            KnownUIContexts.SolutionBuildingContext.UIContextChanged -= SolutionBuildingContextChanged;
            KnownUIContexts.SolutionOpeningContext.UIContextChanged -= SolutionOpeningContextChanged;
            _solutionClosingContext.UIContextChanged -= SolutionClosingContextChanged;
        }

        private void SolutionBuildingContextChanged(object? sender, UIContextChangedEventArgs e)
            => ContextChanged(e.Activated, SolutionBuilding);

        private void SolutionOpeningContextChanged(object? sender, UIContextChangedEventArgs e)
            => ContextChanged(e.Activated, SolutionOpening);

        private void SolutionClosingContextChanged(object? sender, UIContextChangedEventArgs e)
            => ContextChanged(e.Activated, SolutionClosing);

        private void ContextChanged(bool active, string operation)
        {
            TryCancelPendingNotification(operation);

            if (active)
                _operations[operation] = _notificationService.Start(operation);
        }

        private void TryCancelPendingNotification(string operation)
        {
            if (_operations.TryGetValue(operation, out var globalOperation))
            {
                globalOperation.Dispose();
                _operations.Remove(operation);
            }
        }
    }
}
