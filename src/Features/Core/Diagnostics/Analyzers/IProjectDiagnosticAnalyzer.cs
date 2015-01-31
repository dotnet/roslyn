// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// IDE-only project based diagnostic analyzer.
    /// </summary>
    internal abstract class ProjectDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        public abstract Task AnalyzeProjectAsync(Project project, Action<Diagnostic> addDiagnostic, CancellationToken cancellationToken);

        public override void Initialize(AnalysisContext context)
        {
        }
    }
}
