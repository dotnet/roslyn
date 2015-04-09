// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices
{
    /// <summary>
    /// Monitors Visual Studio's UIContext for SolutionBuilding and notifies the GlobalOperationService.
    /// The intent is to suspend analysis of non-essential files for the duration of a build.
    /// </summary>
    internal sealed class SolutionEventMonitor : IDisposable
    {
        private GlobalOperationNotificationService _notificationService;
        private Dictionary<string, GlobalOperationRegistration> _operations = new Dictionary<string, GlobalOperationRegistration>();

        public SolutionEventMonitor(VisualStudioWorkspace workspace)
        {
            var notificationService = workspace.Services.GetService<IGlobalOperationNotificationService>() as GlobalOperationNotificationService;
            if (notificationService != null)
            {
                _notificationService = notificationService;
                KnownUIContexts.SolutionBuildingContext.UIContextChanged += SolutionBuildingContextChanged;
                KnownUIContexts.SolutionOpeningContext.UIContextChanged += SolutionOpeningContextChanged;
            }
        }

        public void Dispose()
        {
            foreach (var globalOperation in _operations.Values)
            {
                globalOperation.Dispose();
            }

            _operations.Clear();

            if (_notificationService != null)
            {
                _notificationService = null;
                KnownUIContexts.SolutionBuildingContext.UIContextChanged -= SolutionBuildingContextChanged;
                KnownUIContexts.SolutionOpeningContext.UIContextChanged -= SolutionOpeningContextChanged;
            }
        }

        private void SolutionBuildingContextChanged(object sender, UIContextChangedEventArgs e)
        {
            ContextChangedWorker(e, "Solution Building");
        }

        private void SolutionOpeningContextChanged(object sender, UIContextChangedEventArgs e)
        {
            ContextChangedWorker(e, "Solution Opening");
        }

        private void ContextChangedWorker(UIContextChangedEventArgs e, string operation)
        {
            if (_notificationService == null)
            {
                return;
            }

            TryCancelPendingNotification(operation);

            if (e.Activated)
            {
                _operations[operation] = _notificationService.Start(operation);
            }
        }

        private void TryCancelPendingNotification(string operation)
        {
            GlobalOperationRegistration globalOperation;
            if (_operations.TryGetValue(operation, out globalOperation))
            {
                globalOperation.Done();
                globalOperation.Dispose();
                _operations.Remove(operation);
            }
        }
    }
}
