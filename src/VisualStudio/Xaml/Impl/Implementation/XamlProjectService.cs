// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Xaml.Diagnostics.Analyzers;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;
using Shell = Microsoft.VisualStudio.Shell;

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
        private readonly Shell.RunningDocumentTable _rdt;
        private readonly IVsSolution _vsSolution;
        private uint? _rdtEventsCookie;
        private readonly Dictionary<IVsHierarchy, VisualStudioProject> _xamlProjects = new();

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
            _rdt = new Shell.RunningDocumentTable(_serviceProvider);
            _vsSolution = (IVsSolution)_serviceProvider.GetService(typeof(SVsSolution));
            _vsSolution.AdviseSolutionEvents(this, out _);

            AnalyzerService = analyzerService;
        }

        public static IXamlDocumentAnalyzerService? AnalyzerService { get; private set; }

        public void TrackOpenDocument(string filePath)
        {
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
            _rdt.FindDocument(filePath, out var hierarchy, out _, out var docCookie);
            if (hierarchy == null)
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

                var documentId = _workspace.CurrentSolution.GetDocumentIdsWithFilePath(filePath).First(d => d.ProjectId == project.Id);
                var document = _workspace.CurrentSolution.GetDocument(documentId)!;
                var hasText = document.TryGetText(out var text);
                if (!hasText || text?.Container.TryGetTextBuffer() == null)
                {
                    var docInfo = _rdt.GetDocumentInfo(docCookie);
                    var textBuffer = _editorAdaptersFactory.GetDocumentBuffer(docInfo.DocData as IVsTextBuffer);
                    var textContainer = textBuffer.AsTextContainer();
                    _workspace.OnDocumentTextChanged(documentId, textContainer.CurrentText, PreservationMode.PreserveIdentity);
                }
            }

            if (!_rdtEventsCookie.HasValue)
            {
                _rdtEventsCookie = _rdt.Advise(this);
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

            var info = _rdt.GetDocumentInfo(docCookie);
            var buffer = TryGetTextBufferFromDocData(info.DocData);
            var isXaml = buffer?.ContentType.IsOfType(ContentTypeNames.XamlContentType) == true;

            // Managed languages rely on the msbuild host object to add and remove documents during rename.
            // For XAML we have to do that ourselves.
            if (project.ContainsSourceFile(oldMoniker))
            {
                project.RemoveSourceFile(oldMoniker);
            }

            if (isXaml)
            {
                project.AddSourceFile(newMoniker);
            }
        }

        private void OnDocumentClosed(uint docCookie)
        {
            var info = _rdt.GetDocumentInfo(docCookie);
            if (info.Hierarchy != null && _xamlProjects.TryGetValue(info.Hierarchy, out var project))
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
        private ITextBuffer? TryGetTextBufferFromDocData(object docData)
        {
            if (docData is IVsTextBuffer vsTestBuffer)
            {
                return _editorAdaptersFactory.GetDocumentBuffer(vsTestBuffer);
            }

            return null;
        }
    }
}
