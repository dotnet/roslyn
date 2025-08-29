// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Extensions;

internal static class IDiagnosticServiceExtensions
{
    public static Task<ImmutableArray<DiagnosticData>> ForceAnalyzeProjectAsync(
        this IDiagnosticAnalyzerService service, Project project, CancellationToken cancellationToken)
    {
        return CodeAnalysisDiagnosticAnalyzerServiceHelpers.ForceCodeAnalysisDiagnosticsAsync(
            service, project, new(), cancellationToken);
    }
}
