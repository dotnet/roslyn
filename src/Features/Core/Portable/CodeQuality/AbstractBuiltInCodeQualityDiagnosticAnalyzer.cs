// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeQuality
{
    internal abstract class AbstractBuiltInCodeQualityDiagnosticAnalyzer : AbstractCodeQualityDiagnosticAnalyzer, IBuiltInAnalyzer
    {
        protected AbstractBuiltInCodeQualityDiagnosticAnalyzer(
            ImmutableArray<DiagnosticDescriptor> descriptors,
            GeneratedCodeAnalysisFlags generatedCodeAnalysisFlags)
            : base(descriptors, generatedCodeAnalysisFlags)
        {
        }

        public abstract DiagnosticAnalyzerCategory GetAnalyzerCategory();

        public bool OpenFileOnly(Workspace workspace)
            => false;
    }
}
