// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.IO.Packaging;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.LanguageServices
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    [Export(typeof(SampleToolWindowFactory))]
    internal class SampleToolWindowFactory : IWpfTextViewCreationListener
    {
        private bool _initialized;

        [MemberNotNullWhen(true, nameof(_initialized))]
        private RoslynPackage? Package { get; set; }
        [MemberNotNullWhen(true, nameof(_initialized))]
        private IThreadingContext? ThreadingContext { get; set; }

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SampleToolWindowFactory()
        {
        }

        public void Initialize(RoslynPackage package)
        {
            Package = package;
            ThreadingContext = package.ComponentModel.GetService<IThreadingContext>();
            _initialized = true;
        }

        /// <summary>
        /// Called whenever a new file is opened
        /// </summary>
        public void TextViewCreated(IWpfTextView textView)
        {
            _ = ShowSampleToolWindowAsync();
        }

        public async Task ShowSampleToolWindowAsync(CancellationToken cancellationToken = default)
        {
            if (!_initialized || ThreadingContext is null)
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
                if (Package is null)
                {
                    throw new NotSupportedException("Cannot create tool window");
                }

                var window = Package.FindToolWindow(typeof(SampleToolWindow), 0, true) as SampleToolWindow;
                if (window is not { Frame: not null })
                {
                    throw new NotSupportedException("Cannot create tool window");
                }

                window.InitializeIfNeeded();

                return window;
            }
        }
    }
}
