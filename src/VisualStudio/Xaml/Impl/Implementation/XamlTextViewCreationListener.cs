// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
using Microsoft.VisualStudio.Text;
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
        private readonly VisualStudioProjectFactory _visualStudioProjectFactory;
        private readonly VisualStudioWorkspaceImpl _vsWorkspace;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactory;
        private readonly Lazy<RunningDocumentTable> _rdt;
        private readonly IVsSolution _vsSolution;
        private uint? _solutionEventsCookie;
        private uint? _rdtEventsCookie;
        private readonly Dictionary<IVsHierarchy, VisualStudioProject> _xamlProjects = new Dictionary<IVsHierarchy, VisualStudioProject>();

        internal ICommandHandlerServiceFactory CommandHandlerServiceFactory { get; }

        [ImportingConstructor]
        public XamlTextViewCreationListener(
            [Import(typeof(SVsServiceProvider))] System.IServiceProvider services,
            ICommandHandlerServiceFactory commandHandlerServiceFactory,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            IXamlDocumentAnalyzerService analyzerService,
            VisualStudioWorkspaceImpl vsWorkspace,
            VisualStudioProjectFactory visualStudioProjectFactory)
        {
            _serviceProvider = services;
            CommandHandlerServiceFactory = commandHandlerServiceFactory;
            _editorAdaptersFactory = editorAdaptersFactoryService;
            _vsWorkspace = vsWorkspace;
            _visualStudioProjectFactory = visualStudioProjectFactory;
            _rdt = new Lazy<RunningDocumentTable>(() => new RunningDocumentTable(_serviceProvider));
            _vsSolution = (IVsSolution)_serviceProvider.GetService(typeof(SVsSolution));

            AnalyzerService = analyzerService;

            if (ErrorHandler.Succeeded(_vsSolution.AdviseSolutionEvents(this, out var solutionEventsCookie)))
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

            if (ErrorHandler.Failed(vsTextView.GetBuffer(out var textLines)))
            {
                return;
            }

            IVsUserData userData = textLines as IVsUserData;
            if (userData == null)
            {
                return;
            }

            Guid monikerGuid = typeof(IVsUserData).GUID;
            if (ErrorHandler.Failed(userData.GetData(ref monikerGuid, out var monikerObj)))
            {
                return;
            }

            string filePath = monikerObj as string;

            _rdt.Value.FindDocument(filePath, out var hierarchy, out var itemId, out var docCookie);
            if (hierarchy == null)
            {
                return;
            }

            VisualStudioProject project;

            if (!_xamlProjects.TryGetValue(hierarchy, out project))
            {
                string name;
                hierarchy.TryGetName(out name);
                project = _visualStudioProjectFactory.CreateAndAddToWorkspace(name + "-XamlProject", StringConstants.XamlLanguageName);
                _xamlProjects.Add(hierarchy, project);
            }

            if (!project.ContainsSourceFile(filePath))
            {
                project.AddSourceFile(filePath);
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

        private void OnProjectClosing(IVsHierarchy hierarchy)
        {
            if (_xamlProjects.TryGetValue(hierarchy, out VisualStudioProject project))
            {
                project.RemoveFromWorkspace();
                _xamlProjects.Remove(hierarchy);
            }
        }

        private void OnDocumentMonikerChanged(uint docCookie, IVsHierarchy hierarchy, string oldMoniker, string newMoniker)
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
            if (!_xamlProjects.TryGetValue(hierarchy, out VisualStudioProject project))
            {
                return;
            }

            // Managed languages rely on the msbuild host object to add and remove documents during rename.
            // For XAML we have to do that ourselves.
            if (project.ContainsSourceFile(oldMoniker))
            {
                project.RemoveSourceFile(oldMoniker);
            }

            var info = _rdt.Value.GetDocumentInfo(docCookie);
            var buffer = TryGetTextBufferFromDocData(info.DocData);

            // If the file extension changed which causes the content type to change
            // (e.g. from .xaml to .cs) we should not add the new document to Xaml project.
            if (buffer != null && buffer.ContentType.IsOfType(ContentTypeNames.XamlContentType))
            {
                project.AddSourceFile(newMoniker);
            }
        }

        private void OnDocumentClosed(uint docCookie)
        {
            RunningDocumentInfo info = _rdt.Value.GetDocumentInfo(docCookie);
            if (info.Hierarchy != null && _xamlProjects.TryGetValue(info.Hierarchy, out VisualStudioProject project))
            {
                if (project.ContainsSourceFile(info.Moniker))
                {
                    project.RemoveSourceFile(info.Moniker);
                }
            }
        }

        /// <summary>
        /// Tries to return an ITextBuffer representing the document from the document's DocData.
        /// </summary>
        /// <param name="docData">The DocData from the running document table.</param>
        /// <returns>The ITextBuffer. If one could not be found, this returns null.</returns>
        private ITextBuffer TryGetTextBufferFromDocData(object docData)
        {
            if (docData is IVsTextBuffer vsTestBuffer)
            {
                return _editorAdaptersFactory.GetDocumentBuffer(vsTestBuffer);
            }
            else
            {
                return null;
            }
        }
    }
}
