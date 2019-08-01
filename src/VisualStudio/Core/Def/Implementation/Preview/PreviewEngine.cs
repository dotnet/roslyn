// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Preview
{
    internal class PreviewEngine : ForegroundThreadAffinitizedObject, IVsPreviewChangesEngine
    {
        private readonly ITextDifferencingSelectorService _diffSelector;
        private readonly IVsEditorAdaptersFactoryService _editorFactory;
        private readonly Solution _newSolution;
        private readonly Solution _oldSolution;
        private readonly string _topLevelName;
        private readonly Glyph _topLevelGlyph;
        private readonly string _helpString;
        private readonly string _description;
        private readonly string _title;
        private readonly IComponentModel _componentModel;
        private readonly IVsImageService2 _imageService;

        private TopLevelChange _topLevelChange;
        private PreviewUpdater _updater;

        public Solution FinalSolution { get; private set; }
        public bool ShowCheckBoxes { get; private set; }

        public PreviewEngine(IThreadingContext threadingContext, string title, string helpString, string description, string topLevelItemName, Glyph topLevelGlyph, Solution newSolution, Solution oldSolution, IComponentModel componentModel, bool showCheckBoxes = true)
            : this(threadingContext, title, helpString, description, topLevelItemName, topLevelGlyph, newSolution, oldSolution, componentModel, null, showCheckBoxes)
        {
        }

        public PreviewEngine(
            IThreadingContext threadingContext,
            string title,
            string helpString,
            string description,
            string topLevelItemName,
            Glyph topLevelGlyph,
            Solution newSolution,
            Solution oldSolution,
            IComponentModel componentModel,
            IVsImageService2 imageService,
            bool showCheckBoxes = true)
            : base(threadingContext)
        {
            _topLevelName = topLevelItemName;
            _topLevelGlyph = topLevelGlyph;
            _title = title ?? throw new ArgumentNullException(nameof(title));
            _helpString = helpString ?? throw new ArgumentNullException(nameof(helpString));
            _description = description ?? throw new ArgumentNullException(nameof(description));
            _newSolution = newSolution.WithMergedLinkedFileChangesAsync(oldSolution, cancellationToken: CancellationToken.None).Result;
            _oldSolution = oldSolution;
            _diffSelector = componentModel.GetService<ITextDifferencingSelectorService>();
            _editorFactory = componentModel.GetService<IVsEditorAdaptersFactoryService>();
            _componentModel = componentModel;
            this.ShowCheckBoxes = showCheckBoxes;
            _imageService = imageService;
        }

        public void CloseWorkspace()
        {
            if (_updater != null)
            {
                _updater.CloseWorkspace();
            }
        }

        public int ApplyChanges()
        {
            FinalSolution = _topLevelChange.GetUpdatedSolution(applyingChanges: true);
            return VSConstants.S_OK;
        }

        public int GetConfirmation(out string pbstrConfirmation)
        {
            pbstrConfirmation = EditorFeaturesResources.Apply2;
            return VSConstants.S_OK;
        }

        public int GetDescription(out string pbstrDescription)
        {
            pbstrDescription = _description;
            return VSConstants.S_OK;
        }

        public int GetHelpContext(out string pbstrHelpContext)
        {
            pbstrHelpContext = _helpString;
            return VSConstants.S_OK;
        }

        public int GetRootChangesList(out object ppIUnknownPreviewChangesList)
        {
            var changes = _newSolution.GetChanges(_oldSolution);
            var projectChanges = changes.GetProjectChanges();

            _topLevelChange = new TopLevelChange(_topLevelName, _topLevelGlyph, _newSolution, _oldSolution, _componentModel, this);

            var builder = ArrayBuilder<AbstractChange>.GetInstance();

            // Documents
            var changedDocuments = projectChanges.SelectMany(p => p.GetChangedDocuments());
            var addedDocuments = projectChanges.SelectMany(p => p.GetAddedDocuments());
            var removedDocuments = projectChanges.SelectMany(p => p.GetRemovedDocuments());

            var allDocumentsWithChanges = new List<DocumentId>();
            allDocumentsWithChanges.AddRange(changedDocuments);
            allDocumentsWithChanges.AddRange(addedDocuments);
            allDocumentsWithChanges.AddRange(removedDocuments);

            // Additional Documents
            var changedAdditionalDocuments = projectChanges.SelectMany(p => p.GetChangedAdditionalDocuments());
            var addedAdditionalDocuments = projectChanges.SelectMany(p => p.GetAddedAdditionalDocuments());
            var removedAdditionalDocuments = projectChanges.SelectMany(p => p.GetRemovedAdditionalDocuments());

            allDocumentsWithChanges.AddRange(changedAdditionalDocuments);
            allDocumentsWithChanges.AddRange(addedAdditionalDocuments);
            allDocumentsWithChanges.AddRange(removedAdditionalDocuments);

            // AnalyzerConfig Documents
            var changedAnalyzerConfigDocuments = projectChanges.SelectMany(p => p.GetChangedAnalyzerConfigDocuments());
            var addedAnalyzerConfigDocuments = projectChanges.SelectMany(p => p.GetAddedAnalyzerConfigDocuments());
            var removedAnalyzerConfigDocuments = projectChanges.SelectMany(p => p.GetRemovedAnalyzerConfigDocuments());

            allDocumentsWithChanges.AddRange(changedAnalyzerConfigDocuments);
            allDocumentsWithChanges.AddRange(addedAnalyzerConfigDocuments);
            allDocumentsWithChanges.AddRange(removedAnalyzerConfigDocuments);

            AppendFileChanges(allDocumentsWithChanges, builder);

            // References (metadata/project/analyzer)
            ReferenceChange.AppendReferenceChanges(projectChanges, this, builder);

            _topLevelChange.Children = builder.Count == 0 ? ChangeList.Empty : new ChangeList(builder.ToArray());
            ppIUnknownPreviewChangesList = _topLevelChange.Children.Changes.Length == 0 ? new ChangeList(new[] { new NoChange(this) }) : new ChangeList(new[] { _topLevelChange });

            if (_topLevelChange.Children.Changes.Length == 0)
            {
                this.ShowCheckBoxes = false;
            }

            return VSConstants.S_OK;
        }

        private void AppendFileChanges(IEnumerable<DocumentId> changedDocuments, ArrayBuilder<AbstractChange> builder)
        {
            // Avoid showing linked changes to linked files multiple times.
            var linkedDocumentIds = new HashSet<DocumentId>();

            var orderedChangedDocuments = changedDocuments.GroupBy(d => d.ProjectId).OrderByDescending(g => g.Count()).Flatten();

            foreach (var documentId in orderedChangedDocuments)
            {
                if (linkedDocumentIds.Contains(documentId))
                {
                    continue;
                }

                var left = _oldSolution.GetTextDocument(documentId);
                var right = _newSolution.GetTextDocument(documentId);

                if (left is Document leftDocument)
                {
                    linkedDocumentIds.AddRange(leftDocument.GetLinkedDocumentIds());
                }
                else if (right is Document rightDocument)
                {
                    // Added document.
                    linkedDocumentIds.AddRange(rightDocument.GetLinkedDocumentIds());
                }

                var fileChange = new FileChange(left, right, _componentModel, _topLevelChange, this, _imageService);
                if (fileChange.Children.Changes.Length > 0)
                {
                    builder.Add(fileChange);
                }
            }
        }

        public int GetTextViewDescription(out string pbstrTextViewDescription)
        {
            pbstrTextViewDescription = EditorFeaturesResources.Preview_Code_Changes_colon;
            return VSConstants.S_OK;
        }

        public int GetTitle(out string pbstrTitle)
        {
            pbstrTitle = _title;
            return VSConstants.S_OK;
        }

        public int GetWarning(out string pbstrWarning, out int ppcwlWarningLevel)
        {
            pbstrWarning = null;
            ppcwlWarningLevel = 0;
            return VSConstants.E_NOTIMPL;
        }

        public void UpdatePreview(DocumentId documentId, SpanChange spanSource)
        {
            var updatedSolution = _topLevelChange.GetUpdatedSolution(applyingChanges: false);
            var document = updatedSolution.GetTextDocument(documentId);
            if (document != null)
            {
                _updater.UpdateView(document, spanSource);
            }
        }

        // We don't get a TexView until they call OnRequestChanges on a child.
        // However, once they've called it once, it's always the same TextView.
        public void SetTextView(object textView)
        {
            if (_updater == null)
            {
                _updater = new PreviewUpdater(ThreadingContext, EnsureTextViewIsInitialized(textView));
            }
        }

        private ITextView EnsureTextViewIsInitialized(object previewTextView)
        {
            // We pass in a regular ITextView in tests
            if (previewTextView != null && previewTextView is ITextView)
            {
                return (ITextView)previewTextView;
            }

            var adapter = (IVsTextView)previewTextView;
            var textView = _editorFactory.GetWpfTextView(adapter);

            if (textView == null)
            {
                EditBufferToInitialize(adapter);
                textView = _editorFactory.GetWpfTextView(adapter);
            }

            return textView;
        }

        // When the dialog is first instantiated, the IVsTextView it contains may 
        // not have been initialized, which will prevent us from using an 
        // EditorAdaptersFactoryService to get the ITextView. If we edit the IVsTextView,
        // it will initialize and we can proceed.
        private static void EditBufferToInitialize(IVsTextView adapter)
        {
            if (adapter == null)
            {
                return;
            }

            var newText = "";
            var newTextPtr = Marshal.StringToHGlobalAuto(newText);
            Marshal.ThrowExceptionForHR(adapter.GetBuffer(out var lines));
            Marshal.ThrowExceptionForHR(lines.GetLastLineIndex(out var piLIne, out var piLineIndex));
            Marshal.ThrowExceptionForHR(lines.GetLengthOfLine(piLineIndex, out var piLineLength));

            Microsoft.VisualStudio.TextManager.Interop.TextSpan[] changes = default;

            piLineLength = piLineLength > 0 ? piLineLength - 1 : 0;

            Marshal.ThrowExceptionForHR(lines.ReplaceLines(0, 0, piLineIndex, piLineLength, newTextPtr, newText.Length, changes));
        }

        private class NoChange : AbstractChange
        {
            public NoChange(PreviewEngine engine) : base(engine)
            {
            }

            public override int GetText(out VSTREETEXTOPTIONS tto, out string ppszText)
            {
                tto = VSTREETEXTOPTIONS.TTO_DEFAULT;
                ppszText = ServicesVSResources.No_Changes;
                return VSConstants.S_OK;
            }

            public override int GetTipText(out VSTREETOOLTIPTYPE eTipType, out string ppszText)
            {
                eTipType = VSTREETOOLTIPTYPE.TIPTYPE_DEFAULT;
                ppszText = null;
                return VSConstants.E_FAIL;
            }

            public override int CanRecurse => 0;
            public override int IsExpandable => 0;

            internal override void GetDisplayData(VSTREEDISPLAYDATA[] pData)
            {
                pData[0].Image = pData[0].SelectedImage = (ushort)StandardGlyphGroup.GlyphInformation;
            }

            public override int OnRequestSource(object pIUnknownTextView)
            {
                return VSConstants.S_OK;
            }

            public override void UpdatePreview()
            {
            }
        }
    }
}
