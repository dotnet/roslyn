// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Preview
{
    internal class TopLevelChange : AbstractChange
    {
        private readonly string _name;
        private readonly IComponentModel _componentModel;
        private Solution _newSolution;
        private Solution _oldSolution;
        private readonly Glyph _glyph;

        public TopLevelChange(
            string name,
            Glyph glyph,
            Solution newSolution,
            Solution oldSolution,
            IComponentModel componentModel,
            PreviewEngine engine)
            : base(engine)
        {
            _name = name;
            _glyph = glyph;
            _componentModel = componentModel;
            _newSolution = newSolution;
            _oldSolution = oldSolution;
        }

        public override int GetText(out VSTREETEXTOPTIONS tto, out string pbstrText)
        {
            pbstrText = _name;
            tto = VSTREETEXTOPTIONS.TTO_DEFAULT;
            return VSConstants.S_OK;
        }

        public override int GetTipText(out VSTREETOOLTIPTYPE eTipType, out string pbstrText)
        {
            throw new NotImplementedException();
        }

        public override int OnRequestSource(object pIUnknownTextView)
        {
            var firstChild = Children.Changes.OfType<FileChange>().First();
            return firstChild.OnRequestSource(pIUnknownTextView);
        }

        public override void UpdatePreview()
        {
            var firstChild = Children.Changes.OfType<FileChange>().First();
            firstChild.UpdatePreview();
        }

        public Solution GetUpdatedSolution(bool applyingChanges)
        {
            var solution = ApplyFileChanges(_newSolution, Children.Changes.OfType<FileChange>(), applyingChanges);
            if (applyingChanges)
            {
                solution = ApplyReferenceChanges(solution, Children.Changes.OfType<ReferenceChange>());
            }

            return solution;
        }

        private Solution ApplyFileChanges(Solution solution, IEnumerable<FileChange> fileChanges, bool applyingChanges)
        {
            foreach (var fileChange in fileChanges)
            {
                var oldTextDocument = fileChange.GetOldDocument();
                var updatedTextDocument = fileChange.GetUpdatedDocument();
                var updatedDocumentTextOpt = updatedTextDocument?.GetTextAsync().Result;

                // Apply file change to document.
                ApplyFileChangesCore(oldTextDocument, updatedTextDocument?.Id, updatedDocumentTextOpt,
                    fileChange.CheckState, fileChange.IsAdditionalDocumentChange);

                // Now apply file change to linked documents.
                if (oldTextDocument is Document oldDocument)
                {
                    foreach (var linkedDocumentId in oldDocument.GetLinkedDocumentIds())
                    {
                        var oldLinkedDocument = oldDocument.Project.Solution.GetDocument(linkedDocumentId);

                        // Ensure that we account for document removal, i.e. updatedDocumentTextOpt == null.
                        var newLinkedDocumentIdOpt = updatedDocumentTextOpt != null ? oldLinkedDocument.Id : null;

                        ApplyFileChangesCore(oldLinkedDocument, newLinkedDocumentIdOpt, updatedDocumentTextOpt,
                            fileChange.CheckState, fileChange.IsAdditionalDocumentChange);
                    }
                }
                else if (updatedTextDocument is Document updatedDocument)
                {
                    foreach (var newLinkedDocumentId in updatedDocument.GetLinkedDocumentIds())
                    {
                        ApplyFileChangesCore(oldTextDocument, newLinkedDocumentId, updatedDocumentTextOpt,
                            fileChange.CheckState, fileChange.IsAdditionalDocumentChange);
                    }
                }
            }

            return solution;

            // Local functions.
            void ApplyFileChangesCore(
                TextDocument oldDocument,
                DocumentId updatedDocumentIdOpt,
                SourceText updateDocumentTextOpt,
                __PREVIEWCHANGESITEMCHECKSTATE checkState,
                bool isAdditionalDoc)
            {
                Debug.Assert(oldDocument != null || updatedDocumentIdOpt != null);
                Debug.Assert((updatedDocumentIdOpt != null) == (updateDocumentTextOpt != null));

                if (oldDocument == null)
                {
                    // Added document to new solution.
                    // If unchecked, then remove this added document from new solution.
                    if (applyingChanges && checkState == __PREVIEWCHANGESITEMCHECKSTATE.PCCS_Unchecked)
                    {
                        solution = isAdditionalDoc ?
                            solution.RemoveAdditionalDocument(updatedDocumentIdOpt) :
                            solution.RemoveDocument(updatedDocumentIdOpt);
                    }
                }
                else if (updatedDocumentIdOpt == null)
                {
                    // Removed document from old solution.
                    // If unchecked, then add back this removed document to new solution.
                    if (applyingChanges && checkState == __PREVIEWCHANGESITEMCHECKSTATE.PCCS_Unchecked)
                    {
                        var oldText = oldDocument.GetTextAsync().Result.ToString();
                        solution = isAdditionalDoc ?
                            solution.AddAdditionalDocument(oldDocument.Id, oldDocument.Name, oldText, oldDocument.Folders, oldDocument.FilePath) :
                            solution.AddDocument(oldDocument.Id, oldDocument.Name, oldText, oldDocument.Folders, oldDocument.FilePath);
                    }
                }
                else
                {
                    Debug.Assert(oldDocument.Id == updatedDocumentIdOpt);

                    // Changed document.
                    solution = isAdditionalDoc ?
                        solution.WithAdditionalDocumentText(updatedDocumentIdOpt, updateDocumentTextOpt) :
                        solution.WithDocumentText(updatedDocumentIdOpt, updateDocumentTextOpt);
                }
            }
        }

        private Solution ApplyReferenceChanges(Solution solution, IEnumerable<ReferenceChange> referenceChanges)
        {
            foreach (var referenceChange in referenceChanges)
            {
                if (referenceChange.IsAddedReference)
                {
                    // Added reference to new solution.
                    // If unchecked, then remove this added reference from new solution.
                    if (referenceChange.CheckState == __PREVIEWCHANGESITEMCHECKSTATE.PCCS_Unchecked)
                    {
                        solution = referenceChange.RemoveFromSolution(solution);
                    }
                }
                else
                {
                    // Removed reference from old solution.
                    // If unchecked, then add back this removed reference to new solution.
                    if (referenceChange.CheckState == __PREVIEWCHANGESITEMCHECKSTATE.PCCS_Unchecked)
                    {
                        solution = referenceChange.AddToSolution(solution);
                    }
                }
            }

            return solution;
        }

        internal override void GetDisplayData(VSTREEDISPLAYDATA[] pData)
        {
            pData[0].Image = pData[0].SelectedImage = (ushort)_glyph.GetStandardGlyphGroup();
        }
    }
}
