// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
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
        protected readonly ContainedDocument ContainedDocument;

        // Set when a TextViewFIlter is set.  We hold onto this to keep our TagSource objects alive even if Venus
        // disconnects the subject buffer from the view temporarily (which they do frequently).  Otherwise, we have to
        // re-compute all of the tag data when they re-connect it, and this causes issues like classification
        // flickering.
        private ITagAggregator<ITag> _bufferTagAggregator;

        // <Previous release> BACKCOMPAT OVERLOAD -- DO NOT TOUCH
        // This is required for the Typescript Language Service
        public ContainedLanguage(
            IVsTextBufferCoordinator bufferCoordinator,
            IComponentModel componentModel,
            AbstractProject project,
            IVsHierarchy hierarchy,
            uint itemid,
            TLanguageService languageService,
            SourceCodeKind sourceCodeKind,
            IFormattingRule vbHelperFormattingRule)
            : this(bufferCoordinator,
                   componentModel,
                   project,
                   hierarchy,
                   itemid,
                   languageService,
                   sourceCodeKind,
                   vbHelperFormattingRule,
                   workspace: null)
        {
        }

        public ContainedLanguage(
            IVsTextBufferCoordinator bufferCoordinator,
            IComponentModel componentModel,
            AbstractProject project,
            IVsHierarchy hierarchy,
            uint itemid,
            TLanguageService languageService,
            SourceCodeKind sourceCodeKind,
            IFormattingRule vbHelperFormattingRule = null,
            Workspace workspace = null)
            : base(project)
        {
            this.BufferCoordinator = bufferCoordinator;
            this.ComponentModel = componentModel;
            _languageService = languageService;

            this.Workspace = workspace ?? componentModel.GetService<VisualStudioWorkspace>();

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

            this.ContainedDocument = new ContainedDocument(
                this, sourceCodeKind, this.Workspace, hierarchy, itemid, componentModel, vbHelperFormattingRule);

            // TODO: Can contained documents be linked or shared?
            this.Project.AddDocument(this.ContainedDocument, isCurrentContext: true, hookupHandlers: true);
            this.DataBuffer.Changed += OnDataBufferChanged;
        }

        private void OnDisconnect()
        {
            this.DataBuffer.Changed -= OnDataBufferChanged;
            this.Project.RemoveDocument(this.ContainedDocument);
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
