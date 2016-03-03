// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer : BaseDiagnosticIncrementalAnalyzer
    {
        public override Task SynchronizeWithBuildAsync(DiagnosticAnalyzerService.BatchUpdateToken token, Project project, ImmutableArray<DiagnosticData> diagnostics)
        {
            // V2 engine doesn't do anything. 
            // it means live error always win over build errors. build errors that can't be reported by live analyzer
            // are already taken cared by engine
            return SpecializedTasks.EmptyTask;
        }

        public override Task SynchronizeWithBuildAsync(DiagnosticAnalyzerService.BatchUpdateToken token, Document document, ImmutableArray<DiagnosticData> diagnostics)
        {
            // V2 engine doesn't do anything. 
            // it means live error always win over build errors. build errors that can't be reported by live analyzer
            // are already taken cared by engine
            return SpecializedTasks.EmptyTask;
        }
    }
}
