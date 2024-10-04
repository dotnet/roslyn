// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Diagnostics
{
    [Shared]
    [Export(typeof(IFSharpDiagnosticAnalyzerService))]
    internal class FSharpDiagnosticAnalyzerService : IFSharpDiagnosticAnalyzerService
    {
        private readonly Microsoft.CodeAnalysis.Diagnostics.IDiagnosticAnalyzerService _delegatee;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FSharpDiagnosticAnalyzerService(Microsoft.CodeAnalysis.Diagnostics.IDiagnosticAnalyzerService delegatee)
        {
            _delegatee = delegatee;
        }

        public void Reanalyze(Workspace workspace, IEnumerable<ProjectId> projectIds = null, IEnumerable<DocumentId> documentIds = null, bool highPriority = false)
            => _delegatee.RequestDiagnosticRefresh();
    }
}
