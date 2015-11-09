// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal sealed class DocumentAnalysisResults
    {
        /// <summary>
        /// Spans of active statements in the document, or null if the document has syntax errors.
        /// </summary>
        public readonly ImmutableArray<LinePositionSpan> ActiveStatements;

        /// <summary>
        /// Diagnostics for rude edits in the document, or null if the document is unchanged or has syntax errors.
        /// If the compilation has semantic errors only syntactic rude edits are calculated.
        /// </summary>
        public readonly ImmutableArray<RudeEditDiagnostic> RudeEditErrors;

        /// <summary>
        /// Edits made in the document, or null if the document is unchanged, has syntax errors, has rude edits,
        /// or if the compilation has semantic errors.
        /// </summary>
        public readonly ImmutableArray<SemanticEdit> SemanticEdits;

        /// <summary>
        /// Exception regions -- spans of catch and finally handlers that surround the active statements.
        /// 
        /// Null if the document has syntax errors or rude edits, or if the compilation has semantic errors.
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
        /// </remarks>
        public readonly ImmutableArray<ImmutableArray<LinePositionSpan>> ExceptionRegions;

        /// <summary>
        /// Line edits in the document, or null if the document has syntax errors or rude edits, 
        /// or if the compilation has semantic errors.
        /// </summary>
        /// <remarks>
        /// Sorted by <see cref="LineChange.OldLine"/>
        /// </remarks>
        public readonly ImmutableArray<LineChange> LineEdits;

        /// <summary>
        /// The compilation has compilation errors (syntactic or semantic), 
        /// or null if the document doesn't have any modifications and
        /// presence of compilation errors was not determined.
        /// </summary>
        private readonly bool? _hasCompilationErrors;

        private DocumentAnalysisResults(ImmutableArray<RudeEditDiagnostic> rudeEdits)
        {
            _hasCompilationErrors = rudeEdits.Length == 0;
            this.RudeEditErrors = rudeEdits;
        }

        public DocumentAnalysisResults(
            ImmutableArray<LinePositionSpan> activeStatements,
            ImmutableArray<RudeEditDiagnostic> rudeEdits,
            ImmutableArray<SemanticEdit> semanticEditsOpt,
            ImmutableArray<ImmutableArray<LinePositionSpan>> exceptionRegionsOpt,
            ImmutableArray<LineChange> lineEditsOpt,
            bool? hasSemanticErrors)
        {
            Debug.Assert(!activeStatements.IsDefault);

            if (hasSemanticErrors.HasValue)
            {
                Debug.Assert(!rudeEdits.IsDefault);

                if (hasSemanticErrors.Value || rudeEdits.Length > 0)
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

                    Debug.Assert(exceptionRegionsOpt.Length == activeStatements.Length);
                }
            }
            else
            {
                Debug.Assert(semanticEditsOpt.IsEmpty);
                Debug.Assert(lineEditsOpt.IsEmpty);

                Debug.Assert(exceptionRegionsOpt.IsDefault || exceptionRegionsOpt.Length == activeStatements.Length);
            }

            this.RudeEditErrors = rudeEdits;
            this.SemanticEdits = semanticEditsOpt;
            this.ActiveStatements = activeStatements;
            this.ExceptionRegions = exceptionRegionsOpt;
            this.LineEdits = lineEditsOpt;
            _hasCompilationErrors = hasSemanticErrors;
        }

        public bool HasChanges
        {
            get
            {
                return _hasCompilationErrors.HasValue;
            }
        }

        public bool HasChangesAndErrors
        {
            get
            {
                return HasChanges && (_hasCompilationErrors.Value || !RudeEditErrors.IsDefaultOrEmpty);
            }
        }

        public bool HasChangesAndCompilationErrors
        {
            get
            {
                return _hasCompilationErrors == true;
            }
        }

        public bool HasSignificantChanges
        {
            get
            {
                return HasChanges && (!SemanticEdits.IsDefaultOrEmpty || !LineEdits.IsDefaultOrEmpty);
            }
        }

        public static DocumentAnalysisResults SyntaxErrors(ImmutableArray<RudeEditDiagnostic> rudeEdits)
        {
            return new DocumentAnalysisResults(rudeEdits);
        }

        public static DocumentAnalysisResults Unchanged(
            ImmutableArray<LinePositionSpan> activeStatements,
            ImmutableArray<ImmutableArray<LinePositionSpan>> exceptionRegionsOpt)
        {
            return new DocumentAnalysisResults(
                activeStatements,
                default(ImmutableArray<RudeEditDiagnostic>),
                ImmutableArray<SemanticEdit>.Empty,
                exceptionRegionsOpt,
                ImmutableArray<LineChange>.Empty,
                hasSemanticErrors: null);
        }

        public static DocumentAnalysisResults Errors(
            ImmutableArray<LinePositionSpan> activeStatements,
            ImmutableArray<RudeEditDiagnostic> rudeEdits,
            bool hasSemanticErrors = false)
        {
            return new DocumentAnalysisResults(
                activeStatements,
                rudeEdits,
                default(ImmutableArray<SemanticEdit>),
                default(ImmutableArray<ImmutableArray<LinePositionSpan>>),
                default(ImmutableArray<LineChange>),
                hasSemanticErrors);
        }

        internal static readonly TraceLog Log = new TraceLog(256, "EnC");
    }
}
