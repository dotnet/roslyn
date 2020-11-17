// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Diagnostics
{
    internal interface IFSharpDiagnosticAnalyzerService
    {
        /// <summary>
        /// re-analyze given projects and documents
        /// </summary>
        void Reanalyze(Workspace workspace, IEnumerable<ProjectId> projectIds = null, IEnumerable<DocumentId> documentIds = null, bool highPriority = false);
    }
}
