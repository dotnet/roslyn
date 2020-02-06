// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Editor.Venus;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;
using IVsContainedLanguageHost = Microsoft.VisualStudio.TextManager.Interop.IVsContainedLanguageHost;
using IVsTextBufferCoordinator = Microsoft.VisualStudio.TextManager.Interop.IVsTextBufferCoordinator;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Venus
{
#pragma warning disable CS0618 // Type or member is obsolete
    internal sealed partial class ContainedDocument : ForegroundThreadAffinitizedObject, IVisualStudioHostDocument
#pragma warning restore CS0618 // Type or member is obsolete
    {
        private static readonly ConcurrentDictionary<DocumentId, ContainedDocument> s_containedDocuments = new ConcurrentDictionary<DocumentId, ContainedDocument>();

        public static ContainedDocument? TryGetContainedDocument(DocumentId? id)
        {
            if (id == null)
            {
                return null;
            }

            s_containedDocuments.TryGetValue(id, out var document);

            return document;
        }

        private readonly IComponentModel _componentModel;
        private readonly Workspace _workspace;
        private readonly ContainedDocumentBuffers _buffers;
        private readonly ReiteratedVersionSnapshotTracker _snapshotTracker;

        public IVsTextBufferCoordinator BufferCoordinator { get; }
        public IVsContainedLanguageHost? ContainedLanguageHost { get; set; }

        public ContainedDocument(
            IThreadingContext threadingContext,
            DocumentId documentId,
            ITextBuffer subjectBuffer,
            ITextBuffer dataBuffer,
            IVsTextBufferCoordinator bufferCoordinator,
            Workspace workspace,
            string language,
            IComponentModel componentModel,
            AbstractFormattingRule? vbHelperFormattingRule)
            : base(threadingContext)
        {
            _componentModel = componentModel;
            _workspace = workspace;

            BufferCoordinator = bufferCoordinator;

            var editorOptionsFactoryService = componentModel.GetService<IEditorOptionsFactoryService>();
            var differenceSelectorService = componentModel.GetService<ITextDifferencingSelectorService>();

            var languageServices = _workspace.Services.GetLanguageServices(language);
            var contentTypeService = languageServices.GetService<IContentTypeLanguageService>();
            var syntaxFacts = languageServices.GetService<ISyntaxFactsService>();

            var diffService = (contentTypeService != null) ? differenceSelectorService.GetTextDifferencingService(contentTypeService.GetDefaultContentType()) : null;
            diffService ??= differenceSelectorService.DefaultTextDifferencingService;

            _snapshotTracker = new ReiteratedVersionSnapshotTracker(subjectBuffer);
            _buffers = new ContainedDocumentBuffers(subjectBuffer, dataBuffer, language, documentId, diffService, editorOptionsFactoryService, syntaxFacts, vbHelperFormattingRule, GetHostLineIndent);

            s_containedDocuments.TryAdd(documentId, this);
        }

        public ITextBuffer SubjectBuffer => _buffers.SubjectBuffer;
        public ITextBuffer DataBuffer => _buffers.DataBuffer;
        public bool SupportsRename => _buffers.SupportsRename;
        public DocumentId Id => _buffers.DocumentId;
        public ProjectId ProjectId => Id.ProjectId;
        public string Language => _buffers.Language;

        [Obsolete("This is a compatibility shim for TypeScript; please do not use it.")]
        internal AbstractProject Project
        {
            get
            {
                return _componentModel.GetService<VisualStudioWorkspaceImpl>().GetProjectTrackerAndInitializeIfNecessary().GetProject(ProjectId);
            }
        }

        [Obsolete("This is a compatibility shim for TypeScript; please do not use it.")]
        internal AbstractContainedLanguage ContainedLanguage
        {
            get
            {
                return new AbstractContainedLanguage(ContainedLanguageHost);
            }
        }

        private string GetHostLineIndent(int lineNumber)
        {
            Contract.ThrowIfNull(ContainedLanguageHost);

            Marshal.ThrowExceptionForHR(
                ContainedLanguageHost.GetLineIndent(
                    lineNumber,
                    out var indent,
                    out _,
                    out _,
                    out _,
                    out _));

            return indent ?? string.Empty;
        }

        public SourceTextContainer GetOpenTextContainer()
        {
            return this.SubjectBuffer.AsTextContainer();
        }

        public void Dispose()
        {
            _snapshotTracker.StopTracking(SubjectBuffer);
            s_containedDocuments.TryRemove(Id, out _);
        }

        public DocumentId? FindProjectDocumentIdWithItemId(uint itemidInsertionPoint)
        {
            var projectId = ProjectId;

            // We cast to VisualStudioWorkspace because the expectation is this isn't being used in Live Share workspaces
            var hierarchy = ((VisualStudioWorkspace)_workspace).GetHierarchy(projectId);

            var project = _workspace.CurrentSolution.GetProject(projectId);
            if (project == null)
            {
                return null;
            }

            foreach (var document in project.Documents)
            {
                if (document.FilePath != null && hierarchy.TryGetItemId(document.FilePath) == itemidInsertionPoint)
                {
                    return document.Id;
                }
            }

            return null;
        }

        public uint FindItemIdOfDocument(DocumentId documentId)
        {
            // We cast to VisualStudioWorkspace because the expectation is this isn't being used in Live Share workspaces
            var hierarchy = ((VisualStudioWorkspace)_workspace).GetHierarchy(ProjectId);
            var document = _workspace.CurrentSolution.GetDocument(documentId);
            if (document == null)
            {
                return 0;
            }

            return hierarchy.TryGetItemId(document.FilePath);
        }

        public void UpdateText(SourceText newText)
           => _buffers.UpdateText(newText, _workspace.CurrentSolution);

        internal IEnumerable<TextChange> FilterFormattedChanges(TextSpan span, IList<TextChange> changes)
            => _buffers.FilterFormattedChanges(span, changes);

        internal AbstractFormattingRule CreateFormattingRule(Document document, int position)
            => _buffers.CreateFormattingRule(document, position);
    }
}
