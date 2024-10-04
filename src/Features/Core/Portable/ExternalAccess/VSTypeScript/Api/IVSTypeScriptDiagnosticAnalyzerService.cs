// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

internal interface IVSTypeScriptDiagnosticAnalyzerService
{
    /// <summary>
    /// Issues a request to invalidate <em>all</em> diagnostics reported to the LSP client, and have them all be
    /// recomputed.  This is equivalent to an LSP diagnostic refresh request.  This should be used sparingly.  For
    /// example: when a user changes an option controlling diagnostics.  Note: all arguments are unused and are only
    /// kept around for legacy binary compat purposes.
    /// </summary>
    void Reanalyze(Workspace? workspace = null, IEnumerable<ProjectId>? projectIds = null, IEnumerable<DocumentId>? documentIds = null, bool highPriority = false);
}
