// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Venus
{
    internal partial class ContainedLanguage
    {
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
        private readonly IDiagnosticAnalyzerService _diagnosticAnalyzerService;
        private readonly Guid _languageServiceGuid;

        protected readonly Workspace Workspace;
        protected readonly IComponentModel ComponentModel;

        public VisualStudioProject? Project { get; }

        protected readonly ContainedDocument ContainedDocument;

        public IVsTextBufferCoordinator BufferCoordinator { get; protected set; }

        /// <summary>
        /// The subject (secondary) buffer that contains the C# or VB code.
        /// </summary>
        public ITextBuffer SubjectBuffer { get; }

        /// <summary>
        /// The underlying buffer that contains C# or VB code. NOTE: This is NOT the "document" buffer
        /// that is saved to disk.  Instead it is the view that the user sees.  The normal buffer graph
        /// in Venus includes 4 buffers:
        /// <code>
        ///            SurfaceBuffer/Databuffer (projection)
        ///             /                               |
        /// Subject Buffer (C#/VB projection)           |
        ///             |                               |
        /// Inert (generated) C#/VB Buffer         Document (aspx) buffer
        /// </code>
        /// In normal circumstance, the Subject and Inert C# buffer are identical in content, and the
        /// Surface and Document are also identical.  The Subject Buffer is the one that is part of the
        /// workspace, that most language operations deal with.  The surface buffer is the one that the
        /// view is created over, and the Document buffer is the one that is saved to disk.
        /// </summary>
        public ITextBuffer DataBuffer { get; }

        // Set when a TextViewFIlter is set.  We hold onto this to keep our TagSource objects alive even if Venus
        // disconnects the subject buffer from the view temporarily (which they do frequently).  Otherwise, we have to
        // re-compute all of the tag data when they re-connect it, and this causes issues like classification
        // flickering.
        private readonly ITagAggregator<ITag> _bufferTagAggregator;

        [Obsolete("This is a compatibility shim for TypeScript; please do not use it.")]
        internal ContainedLanguage(
            IVsTextBufferCoordinator bufferCoordinator,
            IComponentModel componentModel,
            VisualStudioProject? project,
            IVsHierarchy hierarchy,
            uint itemid,
            VisualStudioProjectTracker? projectTrackerOpt,
            ProjectId projectId,
            Guid languageServiceGuid,
            AbstractFormattingRule? vbHelperFormattingRule = null)
            : this(bufferCoordinator,
                   componentModel,
                   projectTrackerOpt?.Workspace ?? componentModel.GetService<VisualStudioWorkspace>(),
                   projectId,
                   project,
                   GetFilePathFromHierarchyAndItemId(hierarchy, itemid),
                   languageServiceGuid,
                   vbHelperFormattingRule)
        {
        }

        public static string GetFilePathFromHierarchyAndItemId(IVsHierarchy hierarchy, uint itemid)
        {
            if (!ErrorHandler.Succeeded(((IVsProject)hierarchy).GetMkDocument(itemid, out var filePath)))
            {
                // we couldn't look up the document moniker from an hierarchy for an itemid.
                // Since we only use this moniker as a key, we could fall back to something else, like the document name.
                Debug.Assert(false, "Could not get the document moniker for an item from its hierarchy.");
                if (!hierarchy.TryGetItemName(itemid, out filePath!))
                {
                    FatalError.Report(new System.Exception("Failed to get document moniker for a contained document"));
                }
            }

            return filePath;
        }

        internal ContainedLanguage(
            IVsTextBufferCoordinator bufferCoordinator,
            IComponentModel componentModel,
            Workspace workspace,
            ProjectId projectId,
            VisualStudioProject? project,
            string filePath,
            Guid languageServiceGuid,
            AbstractFormattingRule? vbHelperFormattingRule = null)
        {
            this.BufferCoordinator = bufferCoordinator;
            this.ComponentModel = componentModel;
            this.Project = project;
            _languageServiceGuid = languageServiceGuid;

            this.Workspace = workspace;

            _editorAdaptersFactoryService = componentModel.GetService<IVsEditorAdaptersFactoryService>();
            _diagnosticAnalyzerService = componentModel.GetService<IDiagnosticAnalyzerService>();

            // Get the ITextBuffer for the secondary buffer
            Marshal.ThrowExceptionForHR(bufferCoordinator.GetSecondaryBuffer(out var secondaryTextLines));
            var secondaryVsTextBuffer = (IVsTextBuffer)secondaryTextLines;
            SubjectBuffer = _editorAdaptersFactoryService.GetDocumentBuffer(secondaryVsTextBuffer);

            // Get the ITextBuffer for the primary buffer
            Marshal.ThrowExceptionForHR(bufferCoordinator.GetPrimaryBuffer(out var primaryTextLines));
            DataBuffer = _editorAdaptersFactoryService.GetDataBuffer((IVsTextBuffer)primaryTextLines);

            // Create our tagger
            var bufferTagAggregatorFactory = ComponentModel.GetService<IBufferTagAggregatorFactoryService>();
            _bufferTagAggregator = bufferTagAggregatorFactory.CreateTagAggregator<ITag>(SubjectBuffer);

            DocumentId documentId;

            if (this.Project != null)
            {
                documentId = this.Project.AddSourceTextContainer(
                    SubjectBuffer.AsTextContainer(), filePath,
                    sourceCodeKind: SourceCodeKind.Regular, folders: default,
                    documentServiceProvider: new ContainedDocument.DocumentServiceProvider(DataBuffer));
            }
            else
            {
                documentId = DocumentId.CreateNewId(projectId, $"{nameof(ContainedDocument)}: {filePath}");

                // We must jam a document into an existing workspace, which we'll assume is safe to do with OnDocumentAdded
                Workspace.OnDocumentAdded(DocumentInfo.Create(documentId, filePath, filePath: filePath));
                Workspace.OnDocumentOpened(documentId, SubjectBuffer.AsTextContainer());
            }

            this.ContainedDocument = new ContainedDocument(
                componentModel.GetService<IThreadingContext>(),
                documentId,
                subjectBuffer: SubjectBuffer,
                dataBuffer: DataBuffer,
                bufferCoordinator,
                this.Workspace,
                project,
                componentModel,
                vbHelperFormattingRule);

            // TODO: Can contained documents be linked or shared?
            this.DataBuffer.Changed += OnDataBufferChanged;
        }

        private void OnDisconnect()
        {
            this.DataBuffer.Changed -= OnDataBufferChanged;

            if (this.Project != null)
            {
                this.Project.RemoveSourceTextContainer(SubjectBuffer.AsTextContainer());
            }
            else
            {
                // It's possible the host of the workspace might have already removed the entire project
                if (Workspace.CurrentSolution.ContainsDocument(ContainedDocument.Id))
                {
                    Workspace.OnDocumentRemoved(ContainedDocument.Id);
                }
            }

            this.ContainedDocument.Dispose();

            if (_bufferTagAggregator != null)
            {
                _bufferTagAggregator.Dispose();
            }
        }

        private void OnDataBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            // we don't actually care what has changed in primary buffer. we just want to re-analyze secondary buffer
            // when primary buffer has changed to update diagnostic positions.
            _diagnosticAnalyzerService.Reanalyze(this.Workspace, documentIds: SpecializedCollections.SingletonEnumerable(this.ContainedDocument.Id));
        }
    }
}
