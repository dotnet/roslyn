// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Preview;

internal class TopLevelChange : AbstractChange
{
    private readonly string _name;
    private readonly Solution _newSolution;
    private readonly Glyph _glyph;

    public TopLevelChange(
        string name,
        Glyph glyph,
        Solution newSolution,
        PreviewEngine engine)
        : base(engine)
    {
        _name = name;
        _glyph = glyph;
        _newSolution = newSolution;
    }

    public override int GetText(out VSTREETEXTOPTIONS tto, out string pbstrText)
    {
        pbstrText = _name;
        tto = VSTREETEXTOPTIONS.TTO_DEFAULT;
        return VSConstants.S_OK;
    }

    public override int GetTipText(out VSTREETOOLTIPTYPE eTipType, out string pbstrText)
        => throw new NotImplementedException();

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

    private static Solution ApplyFileChanges(Solution solution, IEnumerable<FileChange> fileChanges, bool applyingChanges)
    {
        foreach (var fileChange in fileChanges)
        {
            var oldTextDocument = fileChange.GetOldDocument();
            var updatedTextDocument = fileChange.GetUpdatedDocument();
            var updatedDocumentTextOpt = updatedTextDocument?.GetTextSynchronously(CancellationToken.None);

            // Apply file change to document.
            ApplyFileChangesCore(oldTextDocument, updatedTextDocument?.Id, updatedDocumentTextOpt,
                fileChange.CheckState, fileChange.ChangedDocumentKind);

            // Now apply file change to linked documents.
            if (oldTextDocument is Document oldDocument)
            {
                foreach (var linkedDocumentId in oldDocument.GetLinkedDocumentIds())
                {
                    var oldLinkedDocument = oldDocument.Project.Solution.GetDocument(linkedDocumentId);

                    // Ensure that we account for document removal, i.e. updatedDocumentTextOpt == null.
                    var newLinkedDocumentIdOpt = updatedDocumentTextOpt != null ? oldLinkedDocument.Id : null;

                    ApplyFileChangesCore(oldLinkedDocument, newLinkedDocumentIdOpt, updatedDocumentTextOpt,
                        fileChange.CheckState, fileChange.ChangedDocumentKind);
                }
            }
            else if (updatedTextDocument is Document updatedDocument)
            {
                foreach (var newLinkedDocumentId in updatedDocument.GetLinkedDocumentIds())
                {
                    ApplyFileChangesCore(oldTextDocument, newLinkedDocumentId, updatedDocumentTextOpt,
                        fileChange.CheckState, fileChange.ChangedDocumentKind);
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
            TextDocumentKind changedDocumentKind)
        {
            Debug.Assert(oldDocument != null || updatedDocumentIdOpt != null);
            Debug.Assert((updatedDocumentIdOpt != null) == (updateDocumentTextOpt != null));

            if (oldDocument == null)
            {
                // Added document to new solution.
                // If unchecked, then remove this added document from new solution.
                if (applyingChanges && checkState == __PREVIEWCHANGESITEMCHECKSTATE.PCCS_Unchecked)
                {
                    switch (changedDocumentKind)
                    {
                        case TextDocumentKind.Document:
                            solution = solution.RemoveDocument(updatedDocumentIdOpt);
                            break;

                        case TextDocumentKind.AnalyzerConfigDocument:
                            solution = solution.RemoveAnalyzerConfigDocument(updatedDocumentIdOpt);
                            break;

                        case TextDocumentKind.AdditionalDocument:
                            solution = solution.RemoveAdditionalDocument(updatedDocumentIdOpt);
                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(changedDocumentKind);
                    }
                }
            }
            else if (updatedDocumentIdOpt == null)
            {
                // Removed document from old solution.
                // If unchecked, then add back this removed document to new solution.
                if (applyingChanges && checkState == __PREVIEWCHANGESITEMCHECKSTATE.PCCS_Unchecked)
                {
                    var oldText = oldDocument.GetTextSynchronously(CancellationToken.None).ToString();

                    switch (changedDocumentKind)
                    {
                        case TextDocumentKind.Document:
                            solution = solution.AddDocument(oldDocument.Id, oldDocument.Name, oldText, oldDocument.Folders, oldDocument.FilePath);
                            break;

                        case TextDocumentKind.AnalyzerConfigDocument:
                            solution = solution.AddAnalyzerConfigDocument(oldDocument.Id, oldDocument.Name, SourceText.From(oldText), oldDocument.Folders, oldDocument.FilePath);
                            break;

                        case TextDocumentKind.AdditionalDocument:
                            solution = solution.AddAdditionalDocument(oldDocument.Id, oldDocument.Name, oldText, oldDocument.Folders, oldDocument.FilePath);
                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(changedDocumentKind);
                    }
                }
            }
            else
            {
                Debug.Assert(oldDocument.Id == updatedDocumentIdOpt);

                // Changed document.
                solution = solution.WithTextDocumentText(updatedDocumentIdOpt, updateDocumentTextOpt, mode: PreservationMode.PreserveValue);
            }
        }
    }

    private static Solution ApplyReferenceChanges(Solution solution, IEnumerable<ReferenceChange> referenceChanges)
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
        => pData[0].Image = pData[0].SelectedImage = (ushort)_glyph.GetStandardGlyphGroup();
}
