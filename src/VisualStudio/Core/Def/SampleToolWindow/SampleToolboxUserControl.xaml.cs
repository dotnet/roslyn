// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.QuickInfo;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices
{
    /// <summary>
    /// Interaction logic for SampleToolboxUserControl.xaml
    /// </summary>
    internal partial class SampleToolboxUserControl : UserControl
    {
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactory;

        private readonly IVsRunningDocumentTable4 _runningDocumentTable;

        public SampleToolboxUserControl()
        {
            InitializeComponent();
        }

        private Workspace workspace { get; set; }

        internal void InitializeIfNeeded(Workspace workspace, IDocumentTrackingService documentTrackingService)
        {
            this.workspace = workspace;
            documentTrackingService.ActiveDocumentChanged += DocumentTrackingService_ActiveDocumentChanged;
        }

        private void DocumentTrackingService_ActiveDocumentChanged(object sender, DocumentId? documentId)
        {
            var document = workspace.CurrentSolution.GetDocument(documentId);
            var path = document.FilePath;

            document.TryGetText(out var text);
            var con = text.Container;
            var textBuffer = con.TryGetTextBuffer();
            var isCorrectType = textBuffer.ContentType.IsOfType(ContentTypeNames.RoslynContentType);

            if (isCorrectType)
            {
                // make LSP request
                var languageServerName = WellKnownLspServerKinds.AlwaysActiveVSLspServer;
                var lspService = workspace.Services.GetRequiredService<IEmilyService>();
            }
        }
    }

    internal interface IEmilyService : IWorkspaceService
    {
        public int ReinvokeRequestOnServer();
    }

    [ExportWorkspaceServiceFactory(typeof(IEmilyService))]
    [Shared]
    internal class EmilyServiceFactory : IWorkspaceServiceFactory
    {
        private readonly Microsoft.VisualStudio.LanguageServer.ClientILanguageServiceBroker2 _languageServiceBroker;

        [System.Composition.ImportingConstructor]
        public EmilyServiceFactory(ILanguageServiceBroker2 languageServiceBroker)
        {
            // Pull from mef services
            _languageServiceBroker = languageServiceBroker;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            // Create an implementation of IEmilyService and return it
            // pull from workspace services

            //var workspaceService2 = workspaceServices.GetRequiredService<someworkspaceservice>();
            return new EmilyService(_languageServiceBroker);
        }
    }

    internal class EmilyService : IEmilyService
    {
        private readonly ILanguageServiceBroker2 _languageServiceBroker;

        public EmilyService(ILanguageServiceBroker2 languageServiceBroker)
        {
            _languageServiceBroker = languageServiceBroker;
        }

        public int ReinvokeRequestOnServer()
        {
            throw new NotImplementedException();
        }
    }
}
