// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        /// <summary>
        /// enum for each analysis kind.
        /// </summary>
        private enum AnalysisKind
        {
            Syntax,
            Semantic,
            NonLocal
        }
    }
}
