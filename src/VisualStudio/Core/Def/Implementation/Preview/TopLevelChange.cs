// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
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
            foreach (FileChange fileChange in fileChanges)
            {
                var oldDocument = fileChange.GetOldDocument();
                var updatedDocument = fileChange.GetUpdatedDocument();
                bool isAdditionalDoc = fileChange.IsAdditionalDocumentChange;

                if (oldDocument == null)
                {
                    // Added document to new solution.
                    // If unchecked, then remove this added document from new solution.
                    if (applyingChanges && fileChange.CheckState == __PREVIEWCHANGESITEMCHECKSTATE.PCCS_Unchecked)
                    {
                        solution = isAdditionalDoc ?
                            solution.RemoveAdditionalDocument(updatedDocument.Id) :
                            solution.RemoveDocument(updatedDocument.Id);
                    }
                }
                else if (updatedDocument == null)
                {
                    // Removed document from old solution.
                    // If unchecked, then add back this removed document to new solution.
                    if (applyingChanges && fileChange.CheckState == __PREVIEWCHANGESITEMCHECKSTATE.PCCS_Unchecked)
                    {
                        var oldText = oldDocument.GetTextAsync().Result.ToString();
                        solution = isAdditionalDoc ?
                            solution.AddAdditionalDocument(oldDocument.Id, oldDocument.Name, oldText, oldDocument.Folders, oldDocument.FilePath) :
                            solution.AddDocument(oldDocument.Id, oldDocument.Name, oldText, oldDocument.Folders, oldDocument.FilePath);
                    }
                }
                else
                {
                    // Changed document.
                    solution = isAdditionalDoc ?
                        solution.WithAdditionalDocumentText(updatedDocument.Id, updatedDocument.GetTextAsync().Result) :
                        solution.WithDocumentText(updatedDocument.Id, updatedDocument.GetTextAsync().Result);
                }
            }

            return solution;
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
