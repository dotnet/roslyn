// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.InteractiveWindow.Shell;

namespace Microsoft.VisualStudio.LanguageServices.Debugging
{
    [Export(typeof(VsImmediateWindowProvider))]
    internal sealed class VsImmediateWindowProvider
    {
        private readonly IVsInteractiveWindowFactory _vsInteractiveWindowFactory;
        private readonly IVsDebugger _vsDebugger;

        private IVsInteractiveWindow _vsImmediateWindow;

        [ImportingConstructor]
        public VsImmediateWindowProvider(
            SVsServiceProvider serviceProvider,
            IVsInteractiveWindowFactory interactiveWindowFactory,
            IViewClassifierAggregatorService classifierAggregator,
            IContentTypeRegistryService contentTypeRegistry,
            VisualStudioWorkspace workspace)
        {
            _vsInteractiveWindowFactory = interactiveWindowFactory;
            _vsDebugger = (IVsDebugger)serviceProvider.GetService(typeof(IVsDebugger));
        }

        public IVsInteractiveWindow Create()
        {
            var evaluator = new DebuggerEvaluator(_vsDebugger);

            // TODO: localize
            var vsWindow = _vsInteractiveWindowFactory.Create(VsImmediateWindowPackage.Id, 0, "Immediate Window", evaluator, 0);

            // the tool window now owns the engine:
            vsWindow.InteractiveWindow.TextView.Closed += new EventHandler((_, __) => evaluator.Dispose());

            var window = vsWindow.InteractiveWindow;

            // fire and forget:
            window.InitializeAsync();

            return vsWindow;
        }

        public IVsInteractiveWindow Open(bool focus)
        {
            _vsImmediateWindow = _vsImmediateWindow ?? Create();
            _vsImmediateWindow.Show(focus);

            return _vsImmediateWindow;
        }
    }
}
