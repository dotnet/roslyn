// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer : BaseDiagnosticIncrementalAnalyzer
    {
        // TODO: this API will be changed to 1 API that gives all diagnostics under a project
        //       and that will simply replace project state
        public override Task SynchronizeWithBuildAsync(DiagnosticAnalyzerService.BatchUpdateToken token, Project project, ImmutableArray<DiagnosticData> diagnostics)
        {
            // TODO: for now, we dont do anything.
            return SpecializedTasks.EmptyTask;
        }

        public override Task SynchronizeWithBuildAsync(DiagnosticAnalyzerService.BatchUpdateToken token, Document document, ImmutableArray<DiagnosticData> diagnostics)
        {
            // TODO: for now, we dont do anything.
            return SpecializedTasks.EmptyTask;
        }
    }
}
