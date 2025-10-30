// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal readonly struct RudeEditDiagnosticsBuilder : IDisposable
{
    public readonly ArrayBuilder<RudeEditDiagnostic> Diagnostics = ArrayBuilder<RudeEditDiagnostic>.GetInstance();

    /// <summary>
    /// Diagnostics that are only reported after after completing the analysis.
    /// These diagnostics might be reported or discarded based on the results of the analysis.
    /// </summary>
    public readonly ArrayBuilder<(RudeEditDiagnostic diagnostic, RudeEditReportingCondition condition)> DeferredDiagnostics = ArrayBuilder<(RudeEditDiagnostic diagnostic, RudeEditReportingCondition condition)>.GetInstance();

    public RudeEditDiagnosticsBuilder()
    {
    }

    public void Dispose()
    {
        Diagnostics.Free();
        DeferredDiagnostics.Free();
    }

    public void Add(RudeEditDiagnostic diagnostic, RudeEditReportingCondition? deferredReportingCondition = null)
    {
        if (deferredReportingCondition.HasValue)
        {
            DeferredDiagnostics.Add((diagnostic, deferredReportingCondition.Value));
        }
        else
        {
            Diagnostics.Add(diagnostic);
        }
    }

    public ImmutableArray<RudeEditDiagnostic> GetAllDiagnostics(Func<RudeEditDiagnostic, RudeEditReportingCondition, bool> includeDeferred)
        => [
            .. Diagnostics,
            .. DeferredDiagnostics.SelectAsArray(item => includeDeferred(item.diagnostic, item.condition), static item => item.diagnostic)
           ];
}
