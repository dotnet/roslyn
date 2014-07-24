// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis
{
    public enum WorkspaceChangeKind
    {
        /// <summary>
        /// The current solution changed for an unspecified reason.
        /// </summary>
        SolutionChanged,

        /// <summary>
        /// A solution was added to the workspace.
        /// </summary>
        SolutionAdded,

        /// <summary>
        /// The current solution was removed from the workspace.
        /// </summary>
        SolutionRemoved,

        /// <summary>
        /// The current solution was cleared of all projects and documents.
        /// </summary>
        SolutionCleared,

        /// <summary>
        /// The current solution was reloaded.
        /// </summary>
        SolutionReloaded,

        /// <summary>
        /// A project was added to the current solution.
        /// </summary>
        ProjectAdded,

        /// <summary>
        /// A project was removed from the current solution.
        /// </summary>
        ProjectRemoved,

        /// <summary>
        /// A project in the current solution was changed.
        /// </summary>
        ProjectChanged,

        /// <summary>
        /// A project in the current solution was reloaded.
        /// </summary>
        ProjectReloaded,

        /// <summary>
        /// A document was added to the current solution.
        /// </summary>
        DocumentAdded,

        /// <summary>
        /// A document was removed from the current solution.
        /// </summary>
        DocumentRemoved,

        /// <summary>
        /// A document in the current solution was reloaded.
        /// </summary>
        DocumentReloaded,

        /// <summary>
        /// A document in the current solution was changed.
        /// </summary>
        DocumentChanged,

        /// <summary>
        /// An additional document was added to the current solution.
        /// </summary>
        AdditionalDocumentAdded,

        /// <summary>
        /// An additional document was removed from the current solution.
        /// </summary>
        AdditionalDocumentRemoved,

        /// <summary>
        /// An additional document in the current solution was reloaded.
        /// </summary>
        AdditionalDocumentReloaded,

        /// <summary>
        /// An additional document in the current solution was changed.
        /// </summary>
        AdditionalDocumentChanged
    }
}