// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using EnvDTE;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Interactive;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.VisualStudio.Services.Interactive;
using System;
using System.ComponentModel.Composition;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Interactive
{
    [ExportInteractive(typeof(AbstractResetInteractiveCommand), ContentTypeNames.CSharpContentType)]
    internal sealed class CSharpVsResetInteractiveFactory
        : AbstractResetInteractiveCommand
    {
        private readonly IComponentModel _componentModel;
        private readonly CSharpVsInteractiveWindowProvider _interactiveWindowProvider;
        private readonly IServiceProvider _serviceProvider;

        [ImportingConstructor]
        public CSharpVsResetInteractiveFactory(
            [Import]CSharpVsInteractiveWindowProvider interactiveWindowProvider,
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            _interactiveWindowProvider = interactiveWindowProvider;
            _serviceProvider = serviceProvider;
            _componentModel = (IComponentModel)GetService(typeof(SComponentModel));
        }

        private object GetService(Type type)
        {
            return _serviceProvider.GetService(type);
        }

        internal override void ExecuteResetInteractive()
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

        protected override string LanguageName
        {
            get { return "C#"; }
        }

        protected override string ProjectKind
        {
            get { return VSLangProj.PrjKind.prjKindCSharpProject; }
        }

        protected override string CreateReference(string referenceName)
        {
            return string.Format("#r \"{0}\"", referenceName);
        }

        protected override string CreateImport(string namespaceName)
        {
            return string.Format("using {0};", namespaceName);
        }
    }
}
