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

            public bool IsEmpty
            {
                get
                {
                    lock (_gate)
                    {
                        return _syntax.Items.IsEmpty && _semantic.Items.IsEmpty;
                    }
                }
            }

            public void ResetVersion()
            {
                lock (_gate)
                {
                    // reset version of cached data so that we can recalculate new data (ex, OnDocumentReset)
                    _syntax = new DocumentAnalysisData(VersionStamp.Default, _syntax.LineCount, _syntax.Items);
                    _semantic = new DocumentAnalysisData(VersionStamp.Default, _semantic.LineCount, _semantic.Items);
                }
            }

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

            public void Save(AnalysisKind kind, DocumentAnalysisData data)
            {
                Contract.ThrowIfFalse(data.OldItems.IsDefault);

                lock (_gate)
                {
                    switch (kind)
                    {
                        case AnalysisKind.Syntax:
                            _syntax = data;
                            return;

                        case AnalysisKind.Semantic:
                            _semantic = data;
                            return;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(kind);
                    }
                }
            }
        }
    }
}
