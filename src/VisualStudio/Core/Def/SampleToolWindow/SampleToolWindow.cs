// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.LanguageServices
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    [Guid(Guids.SampleToolWindowIdString)]
    internal class SampleToolWindow : ToolWindowPane
    {
        private bool _initialized;

        [MemberNotNullWhen(true, nameof(_initialized))]
        public SampleToolboxUserControl SampleUserControl { get; }

        public SampleToolWindow() : base(null)
        {
            Caption = "Document Outline";
            SampleUserControl = new SampleToolboxUserControl();
            Content = SampleUserControl;
        }


        internal void InitializeIfNeeded(Workspace workspace, IDocumentTrackingService service, ILanguageServiceBroker2 languageServiceBroker, IThreadingContext threadingContext)
        {
            if (_initialized)
            {
                return;
            }

            // Do any initialization logic here
            SampleUserControl.InitializeIfNeeded(workspace, service, languageServiceBroker, threadingContext);
            _initialized = true;
        }
    }
}
