// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis;

internal partial class SolutionCompilationState
{
    private partial class CompilationTracker
    {
        private readonly struct CompilationTrackerGeneratorInfo
        {
            public static readonly CompilationTrackerGeneratorInfo Empty = new(
                TextDocumentStates<SourceGeneratedDocumentState>.Empty,
                driver: null,
                documentsAreFinal: false);

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

            public CompilationTrackerGeneratorInfo(
                TextDocumentStates<SourceGeneratedDocumentState> documents,
                GeneratorDriver? driver) : this(documents, driver, documentsAreFinal: false)
            {
            }

            private CompilationTrackerGeneratorInfo(
                TextDocumentStates<SourceGeneratedDocumentState> documents,
                GeneratorDriver? driver,
                bool documentsAreFinal)
            {
                Documents = documents;
                Driver = driver;
                DocumentsAreFinal = documentsAreFinal;
            }

            public CompilationTrackerGeneratorInfo WithDocumentsAreFinal(bool documentsAreFinal)
                => DocumentsAreFinal == documentsAreFinal ? this : new(Documents, Driver, documentsAreFinal);

            public CompilationTrackerGeneratorInfo WithDriver(GeneratorDriver? driver)
                => Driver == driver ? this : new(Documents, driver, DocumentsAreFinal);
        }
    }
}
