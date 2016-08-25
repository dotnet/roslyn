﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// IDE-only project based diagnostic analyzer.
    /// </summary>
    internal abstract class ProjectDiagnosticAnalyzer : DiagnosticAnalyzer
    {
        public abstract Task<ImmutableArray<Diagnostic>> AnalyzeProjectAsync(Project project, CancellationToken cancellationToken);

        /// <summary>
        /// it is not allowed one to implement both ProjectDiagnosticAnalzyer and DiagnosticAnalyzer
        /// </summary>
        public sealed override void Initialize(AnalysisContext context)
        {
        }
    }
}
