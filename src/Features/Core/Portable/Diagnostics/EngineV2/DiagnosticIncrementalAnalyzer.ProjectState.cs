// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer : BaseDiagnosticIncrementalAnalyzer
    {
        // TODO: implement project state
        //       this should hold onto information similar to CompilerDiagnsoticExecutor.AnalysisResult
        //       this should use dependant project version as its version
        //       this should only cache opened file diagnostics in memory, and all diagnostics in other place.
        //       we might just use temporary storage rather than peristant storage. but will see.
        //       now we don't update individual document incrementally.
        //       but some data might comes from active file state.
        private class ProjectState
        {

        }
    }
}
