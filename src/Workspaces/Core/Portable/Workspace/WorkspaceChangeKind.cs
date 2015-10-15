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
        AdditionalDocumentChanged = 16
    }
}
