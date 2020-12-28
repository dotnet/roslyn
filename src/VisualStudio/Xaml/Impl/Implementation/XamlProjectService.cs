// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Xaml.Diagnostics.Analyzers;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Xaml
{
    [Export]
    internal sealed partial class XamlProjectService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Workspace _workspace;
        private readonly VisualStudioProjectFactory _visualStudioProjectFactory;
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactory;
        private readonly IThreadingContext _threadingContext;
        private readonly Dictionary<IVsHierarchy, VisualStudioProject> _xamlProjects = new();
        private readonly Dictionary<uint, DocumentId> _rdtDocumentIds = new();

        private RunningDocumentTable? _rdt;
        private IVsSolution? _vsSolution;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public XamlProjectService(
            [Import(typeof(Shell.SVsServiceProvider))] IServiceProvider serviceProvider,
            IVsEditorAdaptersFactoryService editorAdaptersFactoryService,
            VisualStudioProjectFactory visualStudioProjectFactory,
            VisualStudioWorkspace workspace,
            IXamlDocumentAnalyzerService analyzerService,
            IThreadingContext threadingContext)
        {
            _serviceProvider = serviceProvider;
            _editorAdaptersFactory = editorAdaptersFactoryService;
            _visualStudioProjectFactory = visualStudioProjectFactory;
            _workspace = workspace;
            _threadingContext = threadingContext;

            AnalyzerService = analyzerService;
        }

        public static IXamlDocumentAnalyzerService? AnalyzerService { get; private set; }

        public void TrackOpenDocument(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                // Can't track anything without a path (can happen while diffing)
                return;
            }

            if (_threadingContext.JoinableTaskContext.IsOnMainThread)
            {
                EnsureDocument(filePath);
            }
            else
            {
                _threadingContext.JoinableTaskFactory.Run(async () =>
                {
                    await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync();

                    EnsureDocument(filePath);
                });
            }
        }

        private void EnsureDocument(string filePath)
        {
            if (_rdt == null)
            {
                _rdt = new RunningDocumentTable(_serviceProvider);
                _rdt.Advise(this);
            }

            if (_vsSolution == null)
            {
                _vsSolution = (IVsSolution)_serviceProvider.GetService(typeof(SVsSolution));
                _vsSolution.AdviseSolutionEvents(this, out _);
            }

            IVsHierarchy? hierarchy = null;
            uint docCookie = 0;

            try
            {
                _rdt.FindDocument(filePath, out hierarchy, out _, out docCookie);
            }
            catch (ArgumentException)
            {
                // We only support open documents that are in the RDT already
            }

            if (hierarchy == null || docCookie == 0)
            {
                return;
            }

            if (!_xamlProjects.TryGetValue(hierarchy, out var project))
            {
                if (!hierarchy.TryGetName(out var name))
                {
                    return;
                }

                if (!hierarchy.TryGetGuidProperty(__VSHPROPID.VSHPROPID_ProjectIDGuid, out var projectGuid))
                {
                    return;
                }

                var projectInfo = new VisualStudioProjectCreationInfo
                {
                    Hierarchy = hierarchy,
                    FilePath = hierarchy.TryGetProjectFilePath(),
                    ProjectGuid = projectGuid
                };

                project = _visualStudioProjectFactory.CreateAndAddToWorkspace(name, StringConstants.XamlLanguageName, projectInfo);
                _xamlProjects.Add(hierarchy, project);
            }

            if (!project.ContainsSourceFile(filePath))
            {
                project.AddSourceFile(filePath);

                var documentId = _workspace.CurrentSolution.GetDocumentIdsWithFilePath(filePath).Single(d => d.ProjectId == project.Id);
                _rdtDocumentIds[docCookie] = documentId;

                // Remove the following when https://github.com/dotnet/roslyn/issues/49879 is fixed
                var document = _workspace.CurrentSolution.GetRequiredDocument(documentId);
                var hasText = document.TryGetText(out var text);
                if (!hasText || text?.Container.TryGetTextBuffer() == null)
                {
                    var docInfo = _rdt.GetDocumentInfo(docCookie);
                    var textBuffer = TryGetTextBufferFromDocData(docInfo.DocData);
                    var textContainer = textBuffer?.AsTextContainer();
                    if (textContainer != null)
                    {
                        _workspace.OnDocumentTextChanged(documentId, textContainer.CurrentText, PreservationMode.PreserveIdentity);
                    }
                }
            }
        }

        private void OnProjectClosing(IVsHierarchy hierarchy)
        {
            if (_xamlProjects.TryGetValue(hierarchy, out var project))
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
            if (!_xamlProjects.TryGetValue(hierarchy, out var project))
            {
                return;
            }

            var info = _rdt?.GetDocumentInfo(docCookie);
            var buffer = TryGetTextBufferFromDocData(info?.DocData);
            var isXaml = buffer?.ContentType.IsOfType(ContentTypeNames.XamlContentType) == true;

            // Managed languages rely on the msbuild host object to add and remove documents during rename.
            // For XAML we have to do that ourselves.
            if (project.ContainsSourceFile(oldMoniker))
            {
                project.RemoveSourceFile(oldMoniker);
            }

            _rdtDocumentIds.Remove(docCookie);

            if (isXaml)
            {
                project.AddSourceFile(newMoniker);

                var documentId = _workspace.CurrentSolution.GetDocumentIdsWithFilePath(newMoniker).Single(d => d.ProjectId == project.Id);
                _rdtDocumentIds[docCookie] = documentId;
            }
        }

        private void OnDocumentClosed(uint docCookie)
        {
            if (_rdtDocumentIds.TryGetValue(docCookie, out var documentId))
            {
                var document = _workspace.CurrentSolution.GetDocument(documentId);
                if (document?.FilePath != null)
                {
                    var project = _xamlProjects.Values.SingleOrDefault(p => p.Id == document.Project.Id);
                    project?.RemoveSourceFile(document.FilePath);
                }
                _rdtDocumentIds.Remove(docCookie);
            }
        }

        /// <summary>
        /// Tries to return an ITextBuffer representing the document from the document's DocData.
        /// </summary>
        /// <param name="docData">The DocData from the running document table.</param>
        /// <returns>The ITextBuffer. If one could not be found, this returns null.</returns>
        private ITextBuffer? TryGetTextBufferFromDocData(object? docData)
        {
            if (docData is IVsTextBuffer vsTestBuffer)
            {
                return _editorAdaptersFactory.GetDocumentBuffer(vsTestBuffer);
            }

            return null;
        }
    }
}
