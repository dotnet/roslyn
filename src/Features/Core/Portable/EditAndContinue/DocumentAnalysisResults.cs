// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using System;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal sealed class DocumentAnalysisResults
    {
        /// <summary>
        /// The state of the document the results are calculated for.
        /// </summary>
        public DocumentId DocumentId { get; }

        /// <summary>
        /// Document file path for logging.
        /// </summary>
        public string FilePath;

        /// <summary>
        /// Spans of active statements in the document, or null if the document has syntax errors or has not changed.
        /// Calculated even in presence of rude edits so that the active statements can be rendered in the editor.
        /// </summary>
        public ImmutableArray<ActiveStatement> ActiveStatements { get; }

        /// <summary>
        /// Diagnostics for rude edits in the document, or empty if the document is unchanged or has syntax errors.
        /// If the compilation has semantic errors only syntactic rude edits are calculated.
        /// </summary>
        public ImmutableArray<RudeEditDiagnostic> RudeEditErrors { get; }

        /// <summary>
        /// The first syntax error, or null if the document does not have syntax errors reported by the compiler.
        /// </summary>
        public Diagnostic? SyntaxError { get; }

        /// <summary>
        /// Edits made in the document, or null if the document is unchanged, has syntax errors or rude edits.
        /// </summary>
        public ImmutableArray<SemanticEditInfo> SemanticEdits { get; }

        /// <summary>
        /// Exception regions -- spans of catch and finally handlers that surround the active statements.
        /// 
        /// Null if the document has syntax errors, rude edits or has not changed.
        /// </summary>
        /// <remarks>
        /// Null if there are any rude edit diagnostics.
        /// 
        /// Otherwise, each active statement in <see cref="ActiveStatements"/> has a corresponding slot in <see cref="ExceptionRegions"/>.
        ///
        /// Exception regions for each EH block/clause are marked as |...|.
        ///   try { ... AS ... } |catch { } finally { }|
        ///   try { } |catch { ... AS ... }| finally { }
        ///   try { } catch { } |finally { ... AS ... }|
        /// 
        /// Contains a minimal set of spans that cover the handlers.
        /// For example:
        ///   try { } |finally { try { ... AS ... } catch {  } }|
        ///   try { } |finally { try { } catch { ... AS ... } }|
        ///   try { try { } |finally { ... AS ... }| } |catch { } catch { } finally { }|
        /// </remarks>
        public ImmutableArray<ImmutableArray<SourceFileSpan>> ExceptionRegions { get; }

        /// <summary>
        /// Line edits in the document (or mapped documents), or null if the document has syntax errors, rude edits or has not changed.
        /// </summary>
        /// <remarks>
        /// Grouped by file name and updates in each group are ordered by <see cref="SourceLineUpdate.OldLine"/>. 
        /// Each entry in the group applies the delta of <see cref="SourceLineUpdate.NewLine"/> - <see cref="SourceLineUpdate.OldLine"/>
        /// to all lines in range [<see cref="SourceLineUpdate.OldLine"/>, next entry's <see cref="SourceLineUpdate.OldLine"/>).
        /// </remarks>
        public ImmutableArray<SequencePointUpdates> LineEdits { get; }

        /// <summary>
        /// Capabilities that are required for the updates made in this document.
        /// <see cref="EditAndContinueCapabilities.None"/> if the document does not have valid changes.
        /// </summary>
        public EditAndContinueCapabilities RequiredCapabilities { get; }

        /// <summary>
        /// Time span it took to perform the analysis.
        /// </summary>
        public TimeSpan ElapsedTime { get; }

        /// <summary>
        /// Document contains errors that block EnC analysis.
        /// </summary>
        public bool HasSyntaxErrors { get; }

        /// <summary>
        /// Document contains changes.
        /// </summary>
        public bool HasChanges { get; }

        public DocumentAnalysisResults(
            DocumentId documentId,
            string filePath,
            ImmutableArray<ActiveStatement> activeStatementsOpt,
            ImmutableArray<RudeEditDiagnostic> rudeEdits,
            Diagnostic? syntaxError,
            ImmutableArray<SemanticEditInfo> semanticEditsOpt,
            ImmutableArray<ImmutableArray<SourceFileSpan>> exceptionRegionsOpt,
            ImmutableArray<SequencePointUpdates> lineEditsOpt,
            EditAndContinueCapabilities requiredCapabilities,
            TimeSpan elapsedTime,
            bool hasChanges,
            bool hasSyntaxErrors)
        {
            Debug.Assert(!rudeEdits.IsDefault);

            if (hasSyntaxErrors || !hasChanges)
            {
                Debug.Assert(activeStatementsOpt.IsDefault);
                Debug.Assert(semanticEditsOpt.IsDefault);
                Debug.Assert(exceptionRegionsOpt.IsDefault);
                Debug.Assert(lineEditsOpt.IsDefault);
                Debug.Assert(syntaxError != null || !rudeEdits.IsEmpty || !hasChanges);
                Debug.Assert(requiredCapabilities == EditAndContinueCapabilities.None);
            }
            else
            {
                Debug.Assert(!activeStatementsOpt.IsDefault);
                Debug.Assert(syntaxError == null);

                if (!rudeEdits.IsEmpty)
                {
                    Debug.Assert(semanticEditsOpt.IsDefault);
                    Debug.Assert(exceptionRegionsOpt.IsDefault);
                    Debug.Assert(lineEditsOpt.IsDefault);
                    Debug.Assert(requiredCapabilities == EditAndContinueCapabilities.None);
                }
                else
                {
                    Debug.Assert(!semanticEditsOpt.IsDefault);
                    Debug.Assert(!exceptionRegionsOpt.IsDefault);
                    Debug.Assert(!lineEditsOpt.IsDefault);

                    // no duplicate files in line edits:
                    Debug.Assert(lineEditsOpt.Select(edit => edit.FileName).Distinct().Count() == lineEditsOpt.Length);

                    // line updates are sorted:
                    Debug.Assert(lineEditsOpt.All(documentLineEdits => documentLineEdits.LineUpdates.IsSorted(Comparer<SourceLineUpdate>.Create(
                        (x, y) => x.OldLine.CompareTo(y.OldLine)))));

                    Debug.Assert(exceptionRegionsOpt.Length == activeStatementsOpt.Length);
                    Debug.Assert(requiredCapabilities != EditAndContinueCapabilities.None);
                }
            }

            DocumentId = documentId;
            FilePath = filePath;
            RudeEditErrors = rudeEdits;
            SyntaxError = syntaxError;
            SemanticEdits = semanticEditsOpt;
            ActiveStatements = activeStatementsOpt;
            ExceptionRegions = exceptionRegionsOpt;
            LineEdits = lineEditsOpt;
            RequiredCapabilities = requiredCapabilities;
            ElapsedTime = elapsedTime;
            HasSyntaxErrors = hasSyntaxErrors;
            HasChanges = hasChanges;
        }

        public bool HasChangesAndErrors
            => HasChanges && (HasSyntaxErrors || !RudeEditErrors.IsEmpty);

        public bool HasChangesAndSyntaxErrors
            => HasChanges && HasSyntaxErrors;

        public bool HasSignificantValidChanges
            => HasChanges && (!SemanticEdits.IsDefaultOrEmpty || !LineEdits.IsDefaultOrEmpty);

        /// <summary>
        /// Report errors blocking the document analysis.
        /// </summary>
        public static DocumentAnalysisResults SyntaxErrors(DocumentId documentId, string filePath, ImmutableArray<RudeEditDiagnostic> rudeEdits, Diagnostic? syntaxError, TimeSpan elapsedTime, bool hasChanges)
            => new(
                documentId,
                filePath,
                activeStatementsOpt: default,
                rudeEdits,
                syntaxError,
                semanticEditsOpt: default,
                exceptionRegionsOpt: default,
                lineEditsOpt: default,
                EditAndContinueCapabilities.None,
                elapsedTime,
                hasChanges,
                hasSyntaxErrors: true);

        /// <summary>
        /// Report unchanged document results.
        /// </summary>
        public static DocumentAnalysisResults Unchanged(DocumentId documentId, string filePath, TimeSpan elapsedTime)
            => new(
                documentId,
                filePath,
                activeStatementsOpt: default,
                rudeEdits: ImmutableArray<RudeEditDiagnostic>.Empty,
                syntaxError: null,
                semanticEditsOpt: default,
                exceptionRegionsOpt: default,
                lineEditsOpt: default,
                EditAndContinueCapabilities.None,
                elapsedTime,
                hasChanges: false,
                hasSyntaxErrors: false);
    }
}
