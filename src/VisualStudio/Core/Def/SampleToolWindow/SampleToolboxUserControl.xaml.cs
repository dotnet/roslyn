// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
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
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.Extensions;
using Microsoft.VisualStudio.LanguageServices.Setup;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServices
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    /// <summary>
    /// Interaction logic for SampleToolboxUserControl.xaml
    /// </summary>
    internal partial class SampleToolboxUserControl : UserControl
    {
        public SampleToolboxUserControl()
        {
            InitializeComponent();
        }

        private Workspace? workspace { get; set; }

        internal void InitializeIfNeeded(Workspace workspace, IDocumentTrackingService documentTrackingService, ILanguageServiceBroker2 languageServiceBroker/*, JsonSerializer serializer*/, IThreadingContext threadingContext)
        {
            this.workspace = workspace;
            documentTrackingService.ActiveDocumentChanged += DocumentTrackingService_ActiveDocumentChanged;

            async void DocumentTrackingService_ActiveDocumentChanged(object sender, DocumentId? documentId)
            {
                var document = workspace.CurrentSolution.GetDocument(documentId);

                if (document == null)
                {
                    return;
                }

                document.TryGetText(out var text);

                if (text == null)
                {
                    return;
                }

                var con = text.Container;
                var textBuffer = con.GetTextBuffer();
                var isCorrectType = textBuffer.ContentType.IsOfType(ContentTypeNames.RoslynContentType);

                var snapshot = textBuffer.CurrentSnapshot;
                var parameterFactory = new DocumentSymbolParams()
                {
                    TextDocument = new TextDocumentIdentifier()
                    {
                        Uri = document.GetURI()
                    }
                };

                var serializedParams = JToken.FromObject(parameterFactory);
                JToken ParameterFactory(ITextSnapshot _)
                {
                    return serializedParams;
                }

                if (isCorrectType)
                {
                    // make LSP request
                    var languageServerName = WellKnownLspServerKinds.AlwaysActiveVSLspServer.ToUserVisibleString();
                    var lspService = languageServiceBroker;
                    var capabilitiesFilter = (JToken x) => true;
                    var method = Methods.TextDocumentDocumentSymbolName;
                    var cancellationToken = CancellationToken.None;

                    // context.ClientCapabilities?.TextDocument?.DocumentSymbol?.HierarchicalDocumentSymbolSupport == true
                    var response = await lspService.RequestAsync(
                        textBuffer: textBuffer,
                        method: method,
                        capabilitiesFilter: capabilitiesFilter,
                        languageServerName: languageServerName,
                        parameterFactory: ParameterFactory,
                        cancellationToken: cancellationToken).ConfigureAwait(false);

                    if (response is null)
                    {
                        return;
                    }

                    DocSymbol addNodes(DocSymbol newNode, DocumentSymbol[] children)
                    {
                        var newChildren = new ObservableCollection<DocSymbol>();

                        if (children is null || children.Length == 0)
                        {
                            return newNode;
                        }
                        else
                        {
                            for (var i = 0; i < children.Length; i++)
                            {
                                var child = children[i];
                                var newChild = new DocSymbol(child.Name);
                                newChild = addNodes(newChild, child.Children);
                                newChildren.Add(newChild);
                            }

                            newNode.Children = newChildren;
                            return newNode;
                        }
                    }

                    if (response.Response is not null)
                    {
                        var body = response.Response.ToObject<DocumentSymbol[]>();
                        await threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                        var documentSymbols = new List<DocSymbol>();

                        if (body is not null && body.Length > 0)
                        {
                            for (var i = 0; i < body.Length; i++)
                            {
                                var ds = new DocSymbol(body[i].Name);
                                ds = addNodes(ds, body[i].Children);
                                documentSymbols.Add(ds);
                            }
                        }
                        else
                        {
                            var ds = new DocSymbol("Nothing to show here");
                            documentSymbols.Add(ds);
                        }
                        trvFamilies.ItemsSource = documentSymbols;
                    }
                }
            }
        }
    }

    class DocSymbol
    {
        public DocSymbol(string name)
        {
            this.Name = name;
            this.Children = new ObservableCollection<DocSymbol>();
        }

        public string Name { get; set; }

        public ObservableCollection<DocSymbol> Children { get; set; }
    }
}
