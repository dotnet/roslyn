// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.Shell;

namespace Microsoft.VisualStudio.LanguageServices
{
    /// <summary>
    /// Monitors Visual Studio's UIContext for SolutionBuilding and notifies the GlobalOperationService.
    /// The intent is to suspend analysis of non-essential files for the duration of a build.
    /// </summary>
    internal sealed class SolutionBuildMonitor : IDisposable
    {
        private GlobalOperationNotificationService _notificationService;
        private GlobalOperationRegistration _operation;

        public SolutionBuildMonitor(VisualStudioWorkspace workspace)
        {
            var notificationService = workspace.Services.GetService<IGlobalOperationNotificationService>() as GlobalOperationNotificationService;
            if (notificationService != null)
            {
                _notificationService = notificationService;
                KnownUIContexts.SolutionBuildingContext.UIContextChanged += SolutionBuildingContextChanged;
                KnownUIContexts.SolutionOpeningContext.UIContextChanged += SolutionOpeningContext_UIContextChanged;
            }
        }

        public void Dispose()
        {
            if (_operation != null)
            {
                _operation.Dispose();
                _operation = null;
            }

            if (_notificationService != null)
            {
                _notificationService = null;
                KnownUIContexts.SolutionBuildingContext.UIContextChanged -= SolutionBuildingContextChanged;
            }
        }

        private void SolutionBuildingContextChanged(object sender, UIContextChangedEventArgs e)
        {
            ContextChangedWorker(e, "Solution Building");
        }

        private void SolutionOpeningContext_UIContextChanged(object sender, UIContextChangedEventArgs e)
        {
            ContextChangedWorker(e, "Solution Opening");
        }

        private void ContextChangedWorker(UIContextChangedEventArgs e, string operation)
        {
            if (_notificationService != null)
            {
                if (e.Activated)
                {
                    if (_operation != null)
                    {
                        _operation.Dispose();
                    }

                    _operation = _notificationService.Start(operation);
                }
                else if (_operation != null)
                {
                    _operation.Done();
                    _operation.Dispose();
                    _operation = null;
                }
            }
        }
    }
}
