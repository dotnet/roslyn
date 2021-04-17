// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal sealed class DocumentAnalysisResults
    {
        internal static readonly TraceLog Log = new(256, "EnC");

        /// <summary>
        /// The state of the document the results are calculated for.
        /// </summary>
        public DocumentId DocumentId { get; }

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
        /// Line edits in the document, or null if the document has syntax errors, rude edits or has not changed.
        /// </summary>
        /// <remarks>
        /// Sorted by <see cref="SourceLineUpdate.OldLine"/>
        /// </remarks>
        public ImmutableArray<SourceLineUpdate> LineEdits { get; }

        /// <summary>
        /// Document contains errors that block EnC analysis.
        /// </summary>
        public readonly bool HasSyntaxErrors;

        /// <summary>
        /// Document contains changes.
        /// </summary>
        public readonly bool HasChanges;

        public DocumentAnalysisResults(
            DocumentId documentId,
            ImmutableArray<ActiveStatement> activeStatementsOpt,
            ImmutableArray<RudeEditDiagnostic> rudeEdits,
            ImmutableArray<SemanticEditInfo> semanticEditsOpt,
            ImmutableArray<ImmutableArray<SourceFileSpan>> exceptionRegionsOpt,
            ImmutableArray<SourceLineUpdate> lineEditsOpt,
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
            }
            else
            {
                Debug.Assert(!activeStatementsOpt.IsDefault);

                if (rudeEdits.Length > 0)
                {
                    Debug.Assert(semanticEditsOpt.IsDefault);
                    Debug.Assert(exceptionRegionsOpt.IsDefault);
                    Debug.Assert(lineEditsOpt.IsDefault);
                }
                else
                {
                    Debug.Assert(!semanticEditsOpt.IsDefault);
                    Debug.Assert(!exceptionRegionsOpt.IsDefault);
                    Debug.Assert(!lineEditsOpt.IsDefault);

                    Debug.Assert(exceptionRegionsOpt.Length == activeStatementsOpt.Length);
                }
            }

            DocumentId = documentId;
            RudeEditErrors = rudeEdits;
            SemanticEdits = semanticEditsOpt;
            ActiveStatements = activeStatementsOpt;
            ExceptionRegions = exceptionRegionsOpt;
            LineEdits = lineEditsOpt;
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
        public static DocumentAnalysisResults SyntaxErrors(DocumentId documentId, ImmutableArray<RudeEditDiagnostic> rudeEdits, bool hasChanges)
            => new(
                documentId,
                activeStatementsOpt: default,
                rudeEdits: rudeEdits,
                semanticEditsOpt: default,
                exceptionRegionsOpt: default,
                lineEditsOpt: default,
                hasChanges,
                hasSyntaxErrors: true);

        /// <summary>
        /// Report unchanged document results.
        /// </summary>
        public static DocumentAnalysisResults Unchanged(DocumentId documentId)
            => new(
                documentId,
                activeStatementsOpt: default,
                rudeEdits: ImmutableArray<RudeEditDiagnostic>.Empty,
                semanticEditsOpt: default,
                exceptionRegionsOpt: default,
                lineEditsOpt: default,
                hasChanges: false,
                hasSyntaxErrors: false);
    }
}
