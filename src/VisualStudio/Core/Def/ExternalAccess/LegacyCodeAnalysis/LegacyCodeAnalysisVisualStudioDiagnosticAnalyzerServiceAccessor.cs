// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ExternalAccess.LegacyCodeAnalysis.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.ExternalAccess.LegacyCodeAnalysis;

[Export(typeof(ILegacyCodeAnalysisVisualStudioDiagnosticAnalyzerServiceAccessor)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class LegacyCodeAnalysisVisualStudioDiagnosticAnalyzerServiceAccessor(
    IThreadingContext threadingContext,
    IVisualStudioDiagnosticAnalyzerService implementation)
    : ILegacyCodeAnalysisVisualStudioDiagnosticAnalyzerServiceAccessor
{
    public IReadOnlyDictionary<string, IEnumerable<DiagnosticDescriptor>> GetAllDiagnosticDescriptors(IVsHierarchy hierarchyOpt)
        => threadingContext.JoinableTaskFactory.Run(() => implementation.GetAllDiagnosticDescriptorsAsync(hierarchyOpt, CancellationToken.None));

    public void RunAnalyzers(IVsHierarchy hierarchyOpt)
        => implementation.RunAnalyzers(hierarchyOpt);
}
