// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Experiment;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Venus
{
    using Workspace = Microsoft.CodeAnalysis.Workspace;

    internal partial class ContainedLanguage<TPackage, TLanguageService> : AbstractContainedLanguage
        where TPackage : AbstractPackage<TPackage, TLanguageService>
        where TLanguageService : AbstractLanguageService<TPackage, TLanguageService>
    {
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
        private readonly IDiagnosticAnalyzerService _diagnosticAnalyzerService;
        private readonly TLanguageService _languageService;

        protected readonly Workspace Workspace;
        protected readonly IComponentModel ComponentModel;
        protected readonly IVisualStudioHostDocument ContainedDocument;

        // Set when a TextViewFIlter is set.  We hold onto this to keep our TagSource objects alive even if Venus
        // disconnects the subject buffer from the view temporarily (which they do frequently).  Otherwise, we have to
        // re-compute all of the tag data when they re-connect it, and this causes issues like classification
        // flickering.
        private ITagAggregator<ITag> _bufferTagAggregator;

        public ContainedLanguage(
            IVsTextBufferCoordinator bufferCoordinator,
            IComponentModel componentModel,
            AbstractProject project,
            IVsHierarchy hierarchy,
            uint itemid,
            TLanguageService languageService,
            SourceCodeKind sourceCodeKind,
            IDocumentServiceFactory documentServiceFactory,
            IFormattingRule vbHelperFormattingRule = null)
            : base(project)
        {
            this.BufferCoordinator = bufferCoordinator;
            this.ComponentModel = componentModel;
            _languageService = languageService;

            this.Workspace = componentModel.GetService<VisualStudioWorkspace>();

            _editorAdaptersFactoryService = componentModel.GetService<IVsEditorAdaptersFactoryService>();
            _diagnosticAnalyzerService = componentModel.GetService<IDiagnosticAnalyzerService>();

            // Get the ITextBuffer for the secondary buffer
            Marshal.ThrowExceptionForHR(bufferCoordinator.GetSecondaryBuffer(out var secondaryTextLines));
            var secondaryVsTextBuffer = (IVsTextBuffer)secondaryTextLines;
            SetSubjectBuffer(_editorAdaptersFactoryService.GetDocumentBuffer(secondaryVsTextBuffer));

            var bufferTagAggregatorFactory = ComponentModel.GetService<IBufferTagAggregatorFactoryService>();
            _bufferTagAggregator = bufferTagAggregatorFactory.CreateTagAggregator<ITag>(SubjectBuffer);
            Marshal.ThrowExceptionForHR(bufferCoordinator.GetPrimaryBuffer(out var primaryTextLines));
            var primaryVsTextBuffer = (IVsTextBuffer)primaryTextLines;
            var dataBuffer = _editorAdaptersFactoryService.GetDataBuffer(primaryVsTextBuffer);
            SetDataBuffer(dataBuffer);

            if (!ErrorHandler.Succeeded(((IVsProject)hierarchy).GetMkDocument(itemid, out var filePath)))
            {
                // we couldn't look up the document moniker from an hierarchy for an itemid.
                // Since we only use this moniker as a key, we could fall back to something else, like the document name.
                Debug.Assert(false, "Could not get the document moniker for an item from its hierarchy.");
                if (!hierarchy.TryGetItemName(itemid, out filePath))
                {
                    Environment.FailFast("Failed to get document moniker for a contained document");
                }
            }
            
            if (this.Project.GetCurrentDocumentFromPath(filePath) is SimpleContainedDocument existingDocument)
            {
                this.ContainedDocument = existingDocument;
                existingDocument.ProcessOpen(this.SubjectBuffer, isCurrentContext: true);
            }
            else
            {
                this.ContainedDocument = new ContainedDocument(
                    this, sourceCodeKind, this.Workspace, hierarchy, itemid, filePath, componentModel,
                    documentServiceFactory, vbHelperFormattingRule);
                this.Project.AddDocument(this.ContainedDocument, isCurrentContext: true, hookupHandlers: true);
            }

            this.DataBuffer.Changed += OnDataBufferChanged;
        }

        private void OnDisconnect()
        {
            this.DataBuffer.Changed -= OnDataBufferChanged;

            if (this.ContainedDocument is ContainedDocument)
            {
                this.Project.RemoveDocument(this.ContainedDocument);
            }
            else if (this.ContainedDocument is SimpleContainedDocument existingDocument)
            {
                existingDocument.ProcessClose(updateActiveContext: true);
            }
        }

        private void OnDataBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            // we don't actually care what has changed in primary buffer. we just want to re-analyze secondary buffer
            // when primary buffer has changed to update diagnostic positions.
            _diagnosticAnalyzerService.Reanalyze(this.Workspace, documentIds: SpecializedCollections.SingletonEnumerable(this.ContainedDocument.Id));
        }

        public override void Dispose()
        {
            if (_bufferTagAggregator != null)
            {
                _bufferTagAggregator.Dispose();
                _bufferTagAggregator = null;
            }
        }
    }
}
