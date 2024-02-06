// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal partial class SolutionCompilationState
{
    private partial class CompilationTracker
    {
        private readonly struct CompilationTrackerGeneratorInfo
        {
            /// <summary>
            /// The best generated documents we have for the current state. <see cref="DocumentsAreFinal"/>
            /// specifies whether the documents are to be considered final and can be reused, or whether they're from
            /// a prior snapshot which needs to be recomputed.
            /// </summary>
            public readonly TextDocumentStates<SourceGeneratedDocumentState> Documents;

            /// <summary>
            /// The <see cref="GeneratorDriver"/> that was used for the last run, to allow for incremental reuse. May
            /// be null if we don't have generators in the first place, haven't ran generators yet for this project,
            /// or had to get rid of our driver for some reason.
            /// </summary>
            public readonly GeneratorDriver? Driver;

            /// <summary>
            /// Whether the generated documents in <see cref="Documents"/> are final and should not be regenerated. 
            /// It's important that once we've ran generators once we don't want to run them again. Once we've ran
            /// them the first time, those syntax trees are visible from other parts of the Workspaces model; if we
            /// run them a second time we'd end up with new trees which would confuse our snapshot model -- once the
            /// tree has been handed out we can't make a second tree later.
            /// </summary>
            public readonly bool DocumentsAreFinal;

            /// <summary>
            /// Whether the generated documents are frozen and generators should never be ran again, ever, even if a document
            /// is later changed. This is used to ensure that when we produce a frozen solution for partial semantics,
            /// further downstream forking of that solution won't rerun generators. This is because of two reasons:
            /// <list type="number">
            /// <item>Generally once we've produced a frozen solution with partial semantics, we now want speed rather
            /// than accuracy; a generator running in a later path will still cause issues there.</item>
            /// <item>The frozen solution with partial semantics makes no guarantee that other syntax trees exist or
            /// whether we even have references -- it's pretty likely that running a generator might produce worse results
            /// than what we originally had.</item>
            /// </list>
            /// </summary>
            public readonly bool DocumentsAreFinalAndFrozen;

            public CompilationTrackerGeneratorInfo(
                TextDocumentStates<SourceGeneratedDocumentState> documents,
                GeneratorDriver? driver,
                bool documentsAreFinal) : this(documents, driver, documentsAreFinal, documentsAreFinalAndFrozen: false)
            {
            }

            private CompilationTrackerGeneratorInfo(
                TextDocumentStates<SourceGeneratedDocumentState> documents,
                GeneratorDriver? driver,
                bool documentsAreFinal,
                bool documentsAreFinalAndFrozen)
            {
                Documents = documents;
                Driver = driver;
                DocumentsAreFinal = documentsAreFinal;
                DocumentsAreFinalAndFrozen = documentsAreFinalAndFrozen;

                // If we're frozen, that implies final as well
                Contract.ThrowIfTrue(documentsAreFinalAndFrozen && !documentsAreFinal);
            }

            public CompilationTrackerGeneratorInfo WithDocumentsAreFinal(bool documentsAreFinal)
            {
                // If we're already frozen, then we won't do anything even if somebody calls WithDocumentsAreFinal(false);
                // this for example would happen if we had a frozen snapshot, and then we fork it further with additional changes.
                // In that case we would be calling WithDocumentsAreFinal(false) to force generators to run again, but if we've
                // frozen in partial semantics, we're done running them period. So we'll just keep treating them as final,
                // no matter the wishes of the caller.
                if (DocumentsAreFinalAndFrozen || DocumentsAreFinal == documentsAreFinal)
                    return this;
                else
                    return new(Documents, Driver, documentsAreFinal);
            }

            public CompilationTrackerGeneratorInfo WithDocumentsAreFinalAndFrozen()
            {
                return DocumentsAreFinalAndFrozen ? this : new(Documents, Driver, documentsAreFinal: true, documentsAreFinalAndFrozen: true);
            }

            public CompilationTrackerGeneratorInfo WithDriver(GeneratorDriver? driver)
                => Driver == driver ? this : new(Documents, driver, DocumentsAreFinal, DocumentsAreFinalAndFrozen);
        }
    }
}
