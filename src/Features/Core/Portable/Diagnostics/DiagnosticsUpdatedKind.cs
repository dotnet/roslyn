// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal enum DiagnosticsUpdatedKind
    {
        /// <summary>
        /// Called when the diagnostic analyzer engine decides to remove existing diagnostics.
        /// For example, this can happen when a document is removed from a solution.  In that
        /// case the analyzer engine will delete all diagnostics associated with that document.
        /// Any layers caching diagnostics should listen for these events to know when to 
        /// delete their cached items entirely.
        /// </summary>
        DiagnosticsRemoved,

        /// <summary>
        /// Called when a new set of (possibly empty) diagnostics have been produced.  This
        /// happens through normal editing and processing of files as diagnostic analyzers
        /// produce new diagnostics for documents and projects.
        /// </summary>
        DiagnosticsCreated,
    }
}
