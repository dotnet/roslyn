// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PdbSourceDocument;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.PdbSourceDocument
{
    [Export(typeof(IPdbSourceDocumentLogger)), Shared]
    internal sealed class PdbSourceDocumentOutputWindowLogger : IPdbSourceDocumentLogger
    {
        private static readonly Guid s_outputPaneGuid = new Guid("f543e896-2e9c-48b8-8fac-d1d5030b4b89");
        private IVsOutputWindowPane? _outputPane;

        private readonly IThreadingContext _threadingContext;
        private readonly IServiceProvider _serviceProvider;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PdbSourceDocumentOutputWindowLogger(SVsServiceProvider serviceProvider, IThreadingContext threadingContext)
        {
            _serviceProvider = serviceProvider;
            _threadingContext = threadingContext;
        }

        public void Clear()
        {
            _threadingContext.JoinableTaskFactory.Run(async () =>
            {
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

                GetPane()?.Clear();
            });
        }

        public void Log(string value)
        {
            _threadingContext.JoinableTaskFactory.Run(async () =>
            {
                // Despite being named "ThreadSafe" unfortunately OutputStringThreadSafe does not play well with JTF
                // which the debugger needs for SourceLink
                await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

                var pane = GetPane();
                if (pane == null)
                {
                    return;
                }

                if (pane is IVsOutputWindowPaneNoPump noPumpPane)
                {
                    noPumpPane.OutputStringNoPump(value + Environment.NewLine);
                }
                else
                {
                    pane.OutputStringThreadSafe(value + Environment.NewLine);
                }
            });
        }

        private IVsOutputWindowPane? GetPane()
        {
            _threadingContext.ThrowIfNotOnUIThread();

            if (_outputPane == null)
            {
                var outputWindow = (IVsOutputWindow)_serviceProvider.GetService(typeof(SVsOutputWindow));

                _outputPane = TryCreateOutputPane(outputWindow);
            }

            return _outputPane;
        }

        private IVsOutputWindowPane? TryCreateOutputPane(IVsOutputWindow outputWindow)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            var paneGuid = s_outputPaneGuid;

            if (ErrorHandler.Succeeded(outputWindow.CreatePane(ref paneGuid, ServicesVSResources.Navigate_to_external_sources, fInitVisible: 1, fClearWithSolution: 1)) &&
                ErrorHandler.Succeeded(outputWindow.GetPane(ref paneGuid, out var pane)))
            {
                return pane;
            }

            return null;
        }
    }
}
