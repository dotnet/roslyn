// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis
{
    public enum WorkspaceChangeKind
    {
        /// <summary>
        /// The current solution changed for an unspecified reason.
        /// </summary>
        SolutionChanged = 0,

        /// <summary>
        /// A solution was added to the workspace.
        /// </summary>
        SolutionAdded = 1,

        /// <summary>
        /// The current solution was removed from the workspace.
        /// </summary>
        SolutionRemoved = 2,

        /// <summary>
        /// The current solution was cleared of all projects and documents.
        /// </summary>
        SolutionCleared = 3,

        /// <summary>
        /// The current solution was reloaded.
        /// </summary>
        SolutionReloaded = 4,

        /// <summary>
        /// A project was added to the current solution.
        /// </summary>
        ProjectAdded = 5,

        /// <summary>
        /// A project was removed from the current solution.
        /// </summary>
        ProjectRemoved = 6,

        /// <summary>
        /// A project in the current solution was changed.
        /// </summary>
        ProjectChanged = 7,

        /// <summary>
        /// A project in the current solution was reloaded.
        /// </summary>
        ProjectReloaded = 8,

        /// <summary>
        /// A document was added to the current solution.
        /// </summary>
        DocumentAdded = 9,

        /// <summary>
        /// A document was removed from the current solution.
        /// </summary>
        DocumentRemoved = 10,

        /// <summary>
        /// A document in the current solution was reloaded.
        /// </summary>
        DocumentReloaded = 11,

        /// <summary>
        /// A document in the current solution was changed.
        /// </summary>
        /// <remarks>
        /// When linked files are edited, one <see cref="DocumentChanged"/> event is fired per
        /// linked file. All of these events contain the same OldSolution, and they all contain
        /// the same NewSolution. This is so that we can trigger document change events on all
        /// affected documents without reporting intermediate states in which the linked file
        /// contents do not match. Each <see cref="DocumentChanged"/> event does not represent
        /// an incremental update from the previous event in this special case.
        /// </remarks>
        DocumentChanged = 12,

        /// <summary>
        /// An additional document was added to the current solution.
        /// </summary>
        AdditionalDocumentAdded = 13,

        /// <summary>
        /// An additional document was removed from the current solution.
        /// </summary>
        AdditionalDocumentRemoved = 14,

        /// <summary>
        /// An additional document in the current solution was reloaded.
        /// </summary>
        AdditionalDocumentReloaded = 15,

        /// <summary>
        /// An additional document in the current solution was changed.
        /// </summary>
        AdditionalDocumentChanged = 16,

        /// <summary>
        /// The document in the current solution had is info changed; name, folders, filepath
        /// </summary>
        DocumentInfoChanged = 17,

        /// <summary>
        /// An analyzer config document was added to the current solution.
        /// </summary>
        AnalyzerConfigDocumentAdded = 18,

        /// <summary>
        /// An analyzer config document was removed from the current solution.
        /// </summary>
        AnalyzerConfigDocumentRemoved = 19,

        /// <summary>
        /// An analyzer config document in the current solution was reloaded.
        /// </summary>
        AnalyzerConfigDocumentReloaded = 20,

        /// <summary>
        /// An analyzer config document in the current solution was changed.
        /// </summary>
        AnalyzerConfigDocumentChanged = 21,

    }
}
