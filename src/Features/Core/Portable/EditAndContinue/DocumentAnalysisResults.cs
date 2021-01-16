// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal sealed class DocumentAnalysisResults
    {
        internal static readonly TraceLog Log = new(256, "EnC");

        /// <summary>
        /// Spans of active statements in the document, or null if the document has compilation errors or rude edits.
        /// </summary>
        public ImmutableArray<ActiveStatement> ActiveStatements { get; }

        /// <summary>
        /// Diagnostics for rude edits in the document, or empty if the document is unchanged or has compilation errors.
        /// If the compilation has semantic errors only syntactic rude edits are calculated.
        /// </summary>
        public ImmutableArray<RudeEditDiagnostic> RudeEditErrors { get; }

        /// <summary>
        /// Edits made in the document, or null if the document is unchanged, has compilation errors or rude edits.
        /// </summary>
        public ImmutableArray<SemanticEdit> SemanticEdits { get; }

        /// <summary>
        /// Exception regions -- spans of catch and finally handlers that surround the active statements.
        /// 
        /// Null if the document has compilation errors or rude edits.
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
        public ImmutableArray<ImmutableArray<LinePositionSpan>> ExceptionRegions { get; }

        /// <summary>
        /// Line edits in the document, or null if the document has compilation errors or rude edits.
        /// </summary>
        /// <remarks>
        /// Sorted by <see cref="SourceLineUpdate.OldLine"/>
        /// </remarks>
        public ImmutableArray<SourceLineUpdate> LineEdits { get; }

        /// <summary>
        /// Document contains erros that block EnC analysis.
        /// </summary>
        public readonly bool HasCompilationErrors;

        /// <summary>
        /// Document contains changes.
        /// </summary>
        public readonly bool HasChanges;

        public DocumentAnalysisResults(
            ImmutableArray<ActiveStatement> activeStatementsOpt,
            ImmutableArray<RudeEditDiagnostic> rudeEdits,
            ImmutableArray<SemanticEdit> semanticEditsOpt,
            ImmutableArray<ImmutableArray<LinePositionSpan>> exceptionRegionsOpt,
            ImmutableArray<SourceLineUpdate> lineEditsOpt,
            bool hasChanges,
            bool hasCompilationErrors)
        {
            Debug.Assert(!rudeEdits.IsDefault);

            if (hasCompilationErrors)
            {
                Debug.Assert(activeStatementsOpt.IsDefault);
                Debug.Assert(semanticEditsOpt.IsDefault);
                Debug.Assert(exceptionRegionsOpt.IsDefault);
                Debug.Assert(lineEditsOpt.IsDefault);
            }
            else if (hasChanges)
            {
                if (rudeEdits.Length > 0)
                {
                    Debug.Assert(activeStatementsOpt.IsDefault);
                    Debug.Assert(semanticEditsOpt.IsDefault);
                    Debug.Assert(exceptionRegionsOpt.IsDefault);
                    Debug.Assert(lineEditsOpt.IsDefault);
                }
                else
                {
                    Debug.Assert(!activeStatementsOpt.IsDefault);
                    Debug.Assert(!semanticEditsOpt.IsDefault);
                    Debug.Assert(!exceptionRegionsOpt.IsDefault);
                    Debug.Assert(!lineEditsOpt.IsDefault);

                    Debug.Assert(exceptionRegionsOpt.Length == activeStatementsOpt.Length);
                }
            }
            else
            {
                Debug.Assert(!activeStatementsOpt.IsDefault);
                Debug.Assert(semanticEditsOpt.IsEmpty);
                Debug.Assert(!exceptionRegionsOpt.IsDefault);
                Debug.Assert(lineEditsOpt.IsEmpty);

                Debug.Assert(exceptionRegionsOpt.Length == activeStatementsOpt.Length);
            }

            RudeEditErrors = rudeEdits;
            SemanticEdits = semanticEditsOpt;
            ActiveStatements = activeStatementsOpt;
            ExceptionRegions = exceptionRegionsOpt;
            LineEdits = lineEditsOpt;
            HasCompilationErrors = hasCompilationErrors;
            HasChanges = hasChanges;
        }

        public bool HasChangesAndErrors
            => HasChanges && (HasCompilationErrors || !RudeEditErrors.IsEmpty);

        public bool HasChangesAndCompilationErrors
            => HasChanges && HasCompilationErrors;

        public bool HasSignificantValidChanges
            => HasChanges && (!SemanticEdits.IsDefaultOrEmpty || !LineEdits.IsDefaultOrEmpty);

        public static DocumentAnalysisResults CompilationErrors(bool hasChanges)
            => new(
                activeStatementsOpt: default,
                rudeEdits: ImmutableArray<RudeEditDiagnostic>.Empty,
                semanticEditsOpt: default,
                exceptionRegionsOpt: default,
                lineEditsOpt: default,
                hasChanges,
                hasCompilationErrors: true);

        public static DocumentAnalysisResults Errors(ImmutableArray<RudeEditDiagnostic> rudeEdits)
            => new(
                activeStatementsOpt: default,
                rudeEdits,
                semanticEditsOpt: default,
                exceptionRegionsOpt: default,
                lineEditsOpt: default,
                hasChanges: true,
                hasCompilationErrors: false);

        public static DocumentAnalysisResults Unchanged(ImmutableArray<ActiveStatement> activeStatements, ImmutableArray<ImmutableArray<LinePositionSpan>> exceptionRegions)
            => new(
                activeStatements,
                rudeEdits: ImmutableArray<RudeEditDiagnostic>.Empty,
                semanticEditsOpt: ImmutableArray<SemanticEdit>.Empty,
                exceptionRegions,
                lineEditsOpt: ImmutableArray<SourceLineUpdate>.Empty,
                hasChanges: false,
                hasCompilationErrors: false);
    }
}
