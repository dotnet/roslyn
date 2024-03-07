// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Diagnostics;

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
