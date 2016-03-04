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
        // TODO: implement active file state
        //       this should hold onto local syntax/semantic diagnostics for active file in memory.
        //       this should also hold onto CompilationWithAnalyzer last time used.
        //       this should use syntax/semantic version for its version
        private class ActiveFileState
        {

        }
    }
}
