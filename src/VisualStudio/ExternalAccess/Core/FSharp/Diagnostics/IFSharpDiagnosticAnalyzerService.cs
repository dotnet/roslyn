// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Diagnostics;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Diagnostics;
#endif

internal interface IFSharpDiagnosticAnalyzerService
{
    /// <summary>
    /// re-analyze given projects and documents
    /// </summary>
    void Reanalyze(Workspace workspace, IEnumerable<ProjectId> projectIds = null, IEnumerable<DocumentId> documentIds = null, bool highPriority = false);
}
