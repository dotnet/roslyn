// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        {
            _implementation = implementation;
        }

        public IReadOnlyDictionary<string, IEnumerable<DiagnosticDescriptor>> GetAllDiagnosticDescriptors(IVsHierarchy hierarchyOpt)
            => _implementation.GetAllDiagnosticDescriptors(hierarchyOpt);

        public void RunAnalyzers(IVsHierarchy hierarchyOpt)
            => _implementation.RunAnalyzers(hierarchyOpt);
    }
}
