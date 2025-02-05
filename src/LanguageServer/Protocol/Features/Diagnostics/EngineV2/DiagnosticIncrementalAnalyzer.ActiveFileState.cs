// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        /// <summary>
        /// state that is responsible to hold onto local diagnostics data regarding active/opened files (depends on host)
        /// in memory.
        /// </summary>
        private sealed class ActiveFileState
        {
            private readonly object _gate = new();

            // file state this is for
            public readonly DocumentId DocumentId;

            // analysis data for each kind
            private DocumentAnalysisData _syntax = DocumentAnalysisData.Empty;
            private DocumentAnalysisData _semantic = DocumentAnalysisData.Empty;

            public ActiveFileState(DocumentId documentId)
                => DocumentId = documentId;

            public DocumentAnalysisData GetAnalysisData(AnalysisKind kind)
            {
                lock (_gate)
                {
                    return kind switch
                    {
                        AnalysisKind.Syntax => _syntax,
                        AnalysisKind.Semantic => _semantic,
                        _ => throw ExceptionUtilities.UnexpectedValue(kind)
                    };
                }
            }
        }
    }
}
