// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.LanguageService;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Venus
{
    internal partial class ContainedLanguage<TPackage, TLanguageService, TProject> : AbstractContainedLanguage
        where TPackage : AbstractPackage<TPackage, TLanguageService, TProject>
        where TLanguageService : AbstractLanguageService<TPackage, TLanguageService, TProject>
        where TProject : AbstractProject
    {
        private readonly IVsEditorAdaptersFactoryService _editorAdaptersFactoryService;
        private readonly TLanguageService _languageService;

        protected readonly Workspace Workspace;
        protected readonly new TProject Project;
        protected readonly IComponentModel ComponentModel;
        protected readonly ContainedDocument ContainedDocument;

        // Set when a TextViewFIlter is set.  We hold onto this to keep our TagSource objects alive even if Venus
        // disconnects the subject buffer from the view temporarily (which they do frequently).  Otherwise, we have to
        // re-compute all of the tag data when they re-connect it, and this causes issues like classification
        // flickering.
        private ITagAggregator<ITag> _bufferTagAggregator;

        public ContainedLanguage(
            IVsTextBufferCoordinator bufferCoordinator,
            IComponentModel componentModel,
            TProject project,
            IVsHierarchy hierarchy,
            uint itemid,
            TLanguageService languageService,
            SourceCodeKind sourceCodeKind,
            IFormattingRule vbHelperFormattingRule = null)
            : base(project)
        {
            this.BufferCoordinator = bufferCoordinator;
            this.ComponentModel = componentModel;
            this.Project = project;
            _languageService = languageService;

            this.Workspace = componentModel.GetService<VisualStudioWorkspace>();
            _editorAdaptersFactoryService = componentModel.GetService<IVsEditorAdaptersFactoryService>();

            // Get the ITextBuffer for the secondary buffer
            IVsTextLines secondaryTextLines;
            Marshal.ThrowExceptionForHR(bufferCoordinator.GetSecondaryBuffer(out secondaryTextLines));
            var secondaryVsTextBuffer = (IVsTextBuffer)secondaryTextLines;
            SetSubjectBuffer(_editorAdaptersFactoryService.GetDocumentBuffer(secondaryVsTextBuffer));

            var bufferTagAggregatorFactory = ComponentModel.GetService<IBufferTagAggregatorFactoryService>();
            _bufferTagAggregator = bufferTagAggregatorFactory.CreateTagAggregator<ITag>(SubjectBuffer);

            IVsTextLines primaryTextLines;
            Marshal.ThrowExceptionForHR(bufferCoordinator.GetPrimaryBuffer(out primaryTextLines));
            var primaryVsTextBuffer = (IVsTextBuffer)primaryTextLines;
            var dataBuffer = _editorAdaptersFactoryService.GetDataBuffer(primaryVsTextBuffer);
            SetDataBuffer(dataBuffer);

            this.ContainedDocument = new ContainedDocument(
                this, sourceCodeKind, this.Workspace, hierarchy, itemid, componentModel, vbHelperFormattingRule);

            // TODO: Can contained documents be linked or shared?
            this.Project.AddDocument(this.ContainedDocument, isCurrentContext: true);
        }

        private void OnDisconnect()
        {
            this.Project.RemoveDocument(this.ContainedDocument);
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
