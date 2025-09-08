// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal sealed partial class DiagnosticAnalyzerService
{
    private async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsInProcessAsync(Project project, ImmutableArray<DocumentId> documentIds, ImmutableHashSet<string>? diagnosticIds, bool includeCompilerAnalyzer, bool includeLocalDocumentDiagnostics, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private async Task<ImmutableArray<DiagnosticData>> GetProjectDiagnosticsForIdsInProcessAsync(Project project, ImmutableHashSet<string>? diagnosticIds, bool includeCompilerAnalyzer, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
