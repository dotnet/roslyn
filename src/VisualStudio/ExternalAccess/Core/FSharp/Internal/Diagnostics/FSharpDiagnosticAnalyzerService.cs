// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ExternalAccess.FSharp.Diagnostics;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Internal.Diagnostics;
#else
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Diagnostics;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Diagnostics;
#endif

[Export(typeof(IFSharpDiagnosticAnalyzerService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class FSharpDiagnosticAnalyzerService(IDiagnosticsRefresher refresher) : IFSharpDiagnosticAnalyzerService
{
    public void Reanalyze(Workspace workspace, IEnumerable<ProjectId> projectIds = null, IEnumerable<DocumentId> documentIds = null, bool highPriority = false)
        => refresher.RequestWorkspaceRefresh();
}
