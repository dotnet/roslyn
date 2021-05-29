// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Composition;
using Microsoft.CodeAnalysis.ExternalAccess.LegacyCodeAnalysis.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.Diagnostics;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.ExternalAccess.LegacyCodeAnalysis
{
    [Export(typeof(ILegacyCodeAnalysisVisualStudioDiagnosticAnalyzerServiceAccessor))]
    [Shared]
    internal sealed class LegacyCodeAnalysisVisualStudioDiagnosticAnalyzerServiceAccessor
        : ILegacyCodeAnalysisVisualStudioDiagnosticAnalyzerServiceAccessor
    {
        private readonly IVisualStudioDiagnosticAnalyzerService _implementation;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public LegacyCodeAnalysisVisualStudioDiagnosticAnalyzerServiceAccessor(IVisualStudioDiagnosticAnalyzerService implementation)
            => _implementation = implementation;

        public IReadOnlyDictionary<string, IEnumerable<DiagnosticDescriptor>> GetAllDiagnosticDescriptors(IVsHierarchy hierarchyOpt)
            => _implementation.GetAllDiagnosticDescriptors(hierarchyOpt);

        public void RunAnalyzers(IVsHierarchy hierarchyOpt)
            => _implementation.RunAnalyzers(hierarchyOpt);
    }
}
