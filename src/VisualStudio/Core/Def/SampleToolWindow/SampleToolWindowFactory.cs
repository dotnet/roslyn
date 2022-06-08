// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO.Packaging;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.LanguageServices
{
    using JsonSerializer = Newtonsoft.Json.JsonSerializer;
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [Export(typeof(SampleToolWindowFactory))]
    internal class SampleToolWindowFactory : IWpfTextViewCreationListener
    {
        private readonly VisualStudioWorkspace _workspace;
        private readonly ILanguageServiceBroker2 _languageServiceBroker;
        private readonly JsonSerializer _serializer;

        [MemberNotNullWhen(true, nameof(Package))]
        private bool Initialized { get; set; }

        private RoslynPackage? Package { get; set; }
        private IThreadingContext ThreadingContext { get; }

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SampleToolWindowFactory(
            VisualStudioWorkspace workspace,
            ILanguageServiceBroker2 languageServiceBroker,
            IThreadingContext threadingContext)
        {
            _workspace = workspace;
            _languageServiceBroker = languageServiceBroker;
            _serializer = new JsonSerializer();
            ThreadingContext = threadingContext;
        }

        public void Initialize(RoslynPackage package)
        {
            Package = package;
            Initialized = true;
        }

        /// <summary>
        /// Called whenever a new file is opened
        /// </summary>
        public void TextViewCreated(IWpfTextView textView)
        {
            ThreadingContext.JoinableTaskFactory.RunAsync(() => ShowSampleToolWindowAsync(CancellationToken.None)).FileAndForget("Opening Sample Tool Window");
        }

        public async Task ShowSampleToolWindowAsync(CancellationToken cancellationToken)
        {
            if (!Initialized)
            {
                throw new NotSupportedException("Tool window not initialized");
            }

            await ThreadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var window = GetOrInitializeWindow();
            var windowFrame = (IVsWindowFrame)window.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());

            return;

            SampleToolWindow GetOrInitializeWindow()
            {
                // Get the instance number 0 of this tool window. This window is single instance so this instance
                // is actually the only one.
                // The last flag is set to true so that if the tool window does not exists it will be created.
                var window = Package.FindToolWindow(typeof(SampleToolWindow), 0, true) as SampleToolWindow;
                if (window is not { Frame: not null })
                {
                    throw new NotSupportedException("Cannot create tool window");
                }

                var service = _workspace.Services.GetRequiredService<IDocumentTrackingService>();

                window.InitializeIfNeeded(_workspace, service, _languageServiceBroker, ThreadingContext);

                return window;
            }
        }
    }
}
