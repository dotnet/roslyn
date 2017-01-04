// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.CodeAnalysis.Xaml.Diagnostics.Analyzers;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Xaml
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType(ContentTypeNames.XamlContentType)]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    internal sealed partial class XamlTextViewCreationListener : IVsTextViewCreationListener
    {
        private readonly System.IServiceProvider _serviceProvider;
        private readonly VisualStudioWorkspaceImpl _vsWorkspace;
        private readonly ICommandHandlerServiceFactory _commandHandlerService;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactory;
        private readonly Lazy<RunningDocumentTable> _rdt;
        private readonly IVsSolution _vsSolution;
        private uint? _solutionEventsCookie;
        private uint? _rdtEventsCookie;

        internal ICommandHandlerServiceFactory CommandHandlerServiceFactory
        {
            get
            {
                return _commandHandlerService;
            }
        }

        [ImportingConstructor]
        public XamlTextViewCreationListener(
            [Import(typeof(SVsServiceProvider))] System.IServiceProvider services,
            ICommandHandlerServiceFactory commandHandlerServiceFactory,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            IXamlDocumentAnalyzerService analyzerService,
            VisualStudioWorkspaceImpl vsWorkspace)
        {
            _serviceProvider = services;
            _commandHandlerService = commandHandlerServiceFactory;
            _editorAdaptersFactory = editorAdaptersFactoryService;
            _vsWorkspace = vsWorkspace;
            _rdt = new Lazy<RunningDocumentTable>(() => new RunningDocumentTable(_serviceProvider));
            _vsSolution = (IVsSolution)_serviceProvider.GetService(typeof(SVsSolution));

            AnalyzerService = analyzerService;

            uint solutionEventsCookie;
            if (ErrorHandler.Succeeded(_vsSolution.AdviseSolutionEvents(this, out solutionEventsCookie)))
            {
                _solutionEventsCookie = solutionEventsCookie;
            }
        }

        public static IXamlDocumentAnalyzerService AnalyzerService { get; private set; }

        public void VsTextViewCreated(IVsTextView vsTextView)
        {
            if (vsTextView == null)
            {
                throw new ArgumentNullException(nameof(vsTextView));
            }

            IVsTextLines textLines;
            if (ErrorHandler.Failed(vsTextView.GetBuffer(out textLines)))
            {
                return;
            }

            IVsUserData userData = textLines as IVsUserData;
            if (userData == null)
            {
                return;
            }

            object monikerObj;
            Guid monikerGuid = typeof(IVsUserData).GUID;
            if (ErrorHandler.Failed(userData.GetData(ref monikerGuid, out monikerObj)))
            {
                return;
            }

            string filePath = monikerObj as string;
            uint itemId;
            uint docCookie;
            IVsHierarchy hierarchy;

            _rdt.Value.FindDocument(filePath, out hierarchy, out itemId, out docCookie);

            AbstractProject project = GetXamlProject(hierarchy);
            if (project == null)
            {
                project = new XamlProject(_vsWorkspace.ProjectTracker, hierarchy, _serviceProvider, _vsWorkspace);
            }

            IVisualStudioHostDocument vsDocument = project.GetCurrentDocumentFromPath(filePath);
            if (vsDocument == null)
            {
                if (!TryCreateXamlDocument(project, filePath, out vsDocument))
                {
                    return;
                }

                project.AddDocument(vsDocument, isCurrentContext: true, hookupHandlers: true);
            }

            AttachRunningDocTableEvents();

            var wpfTextView = _editorAdaptersFactory.GetWpfTextView(vsTextView);
            var target = new XamlOleCommandTarget(wpfTextView, CommandHandlerServiceFactory, _editorAdaptersFactory, _serviceProvider);
            target.AttachToVsTextView();
        }

        private void AttachRunningDocTableEvents()
        {
            if (!_rdtEventsCookie.HasValue)
            {
                _rdtEventsCookie = _rdt.Value.Advise(this);
            }
        }

        private AbstractProject GetXamlProject(IVsHierarchy hierarchy)
        {
            return _vsWorkspace.ProjectTracker.ImmutableProjects.FirstOrDefault(p => p.Language == StringConstants.XamlLanguageName && p.Hierarchy == hierarchy);
        }

        private bool TryCreateXamlDocument(AbstractProject project, string filePath, out IVisualStudioHostDocument vsDocument)
        {
            vsDocument = _vsWorkspace.ProjectTracker.DocumentProvider.TryGetDocumentForFile(
                project, filePath, SourceCodeKind.Regular,
                tb => tb.ContentType.IsOfType(ContentTypeNames.XamlContentType),
                _ => SpecializedCollections.EmptyReadOnlyList<string>());

            return vsDocument != null;
        }

        private void OnProjectClosing(IVsHierarchy hierarchy)
        {
            AbstractProject project = GetXamlProject(hierarchy);
            project?.Disconnect();
        }

        private void OnDocumentMonikerChanged(IVsHierarchy hierarchy, string oldMoniker, string newMoniker)
        {
            // If the moniker change only involves casing differences then the project system will
            // not remove & add the file again with the new name, so we should not clear any state.
            // Leaving the old casing in the DocumentKey is safe because DocumentKey equality 
            // checks ignore the casing of the moniker.
            if (oldMoniker.Equals(newMoniker, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // If the moniker change only involves a non-XAML project then ignore it.
            AbstractProject project = GetXamlProject(hierarchy);
            if (project == null)
            {
                return;
            }

            // Managed languages rely on the msbuild host object to add and remove documents during rename.
            // For XAML we have to do that ourselves.
            IVisualStudioHostDocument oldDocument = project.GetCurrentDocumentFromPath(oldMoniker);
            if (oldDocument != null)
            {
                project.RemoveDocument(oldDocument);
            }

            IVisualStudioHostDocument newDocument = project.GetCurrentDocumentFromPath(newMoniker);
            Debug.Assert(newDocument == null, "Why does the renamed document already exist in the project?");
            if (newDocument == null)
            {
                if (TryCreateXamlDocument(project, newMoniker, out newDocument))
                {
                    project.AddDocument(newDocument, isCurrentContext: true, hookupHandlers: true);
                }
            }
        }

        private void OnDocumentClosed(uint docCookie)
        {
            RunningDocumentInfo info = _rdt.Value.GetDocumentInfo(docCookie);
            AbstractProject project = GetXamlProject(info.Hierarchy);
            if (project == null)
            {
                return;
            }

            IVisualStudioHostDocument document = project.GetCurrentDocumentFromPath(info.Moniker);
            if (document == null)
            {
                return;
            }

            project.RemoveDocument(document);
        }
    }
}
