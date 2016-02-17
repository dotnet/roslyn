// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using EnvDTE;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Interactive;
using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace Roslyn.VisualStudio.Services.Interactive
{
    internal abstract class AbstractResetInteractiveCommand : IResetInteractiveCommand
    {
        private readonly IComponentModel _componentModel;
        private readonly VsInteractiveWindowProvider _interactiveWindowProvider;
        private readonly IServiceProvider _serviceProvider;

        protected abstract string LanguageName { get; }
        protected abstract string CreateReference(string referenceName);
        protected abstract string CreateImport(string namespaceName);

        public AbstractResetInteractiveCommand(
            VsInteractiveWindowProvider interactiveWindowProvider,
            IServiceProvider serviceProvider)
        {
            _interactiveWindowProvider = interactiveWindowProvider;
            _serviceProvider = serviceProvider;
            _componentModel = (IComponentModel)GetService(typeof(SComponentModel));
        }

        private object GetService(Type type)
        {
            return _serviceProvider.GetService(type);
        }

        public void ExecuteResetInteractive()
        {
            var resetInteractive = new VsResetInteractive(
                        (DTE)this.GetService(typeof(SDTE)),
                        _componentModel,
                        (IVsMonitorSelection)this.GetService(typeof(SVsShellMonitorSelection)),
                        (IVsSolutionBuildManager)this.GetService(typeof(SVsSolutionBuildManager)),
                        CreateReference,
                        CreateImport);

            var vsInteractiveWindow = _interactiveWindowProvider.Open(instanceId: 0, focus: true);

            EventHandler focusWindow = null;
            focusWindow = (s, e) =>
            {
                // We have to set focus to the Interactive Window *after* the wait indicator is dismissed.
                vsInteractiveWindow.Show(focus: true);
                resetInteractive.ExecutionCompleted -= focusWindow;
            };

            resetInteractive.Execute(vsInteractiveWindow.InteractiveWindow, LanguageName + " Interactive");
            resetInteractive.ExecutionCompleted += focusWindow;
        }
    }
}
