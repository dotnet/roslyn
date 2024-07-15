// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Common;

namespace Microsoft.CodeAnalysis.Diagnostics;

/// <summary>
/// Base type of a type that is used as <see cref="UpdatedEventArgs.Id"/> for live diagnostic
/// </summary>
internal class AnalyzerUpdateArgsId : BuildToolId.Base<DiagnosticAnalyzer>, ISupportLiveUpdate
{
    public DiagnosticAnalyzer Analyzer => _Field1!;

    protected AnalyzerUpdateArgsId(DiagnosticAnalyzer analyzer)
        : base(analyzer)
    {
    }

    public override string BuildTool => Analyzer.GetAnalyzerAssemblyName();
}
