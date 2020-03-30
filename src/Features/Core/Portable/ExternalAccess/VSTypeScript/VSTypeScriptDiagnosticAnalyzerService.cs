// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript
{
    [Shared]
    [Export(typeof(IVSTypeScriptDiagnosticAnalyzerService))]
    internal sealed class VSTypeScriptAnalyzerService : IVSTypeScriptDiagnosticAnalyzerService
    {
        private readonly IDiagnosticAnalyzerService _service;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VSTypeScriptAnalyzerService(IDiagnosticAnalyzerService service)
            => _service = service;

        public void Reanalyze(Workspace workspace, IEnumerable<ProjectId>? projectIds = null, IEnumerable<DocumentId>? documentIds = null, bool highPriority = false)
            => _service.Reanalyze(workspace, projectIds, documentIds, highPriority);
    }
}
