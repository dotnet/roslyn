// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        /// <summary>
        /// state that is responsible to hold onto local diagnostics data regarding active/opened files (depends on host)
        /// in memory.
        /// </summary>
        private class ActiveFileState
        {
            // file state this is for
            public readonly DocumentId DocumentId;

            // analysis data for each kind
            private DocumentAnalysisData _syntax = DocumentAnalysisData.Empty;
            private DocumentAnalysisData _semantic = DocumentAnalysisData.Empty;

            public ActiveFileState(DocumentId documentId)
            {
                DocumentId = documentId;
            }

            public bool IsEmpty => _syntax.Items.IsEmpty && _semantic.Items.IsEmpty;

            public DocumentAnalysisData GetAnalysisData(AnalysisKind kind)
            {
                switch (kind)
                {
                    case AnalysisKind.Syntax:
                        return _syntax;

                    case AnalysisKind.Semantic:
                        return _semantic;

                    default:
                        return Contract.FailWithReturn<DocumentAnalysisData>("Shouldn't reach here");
                }
            }

            public void Save(AnalysisKind kind, DocumentAnalysisData data)
            {
                Contract.ThrowIfFalse(data.OldItems.IsDefault);

                switch (kind)
                {
                    case AnalysisKind.Syntax:
                        _syntax = data;
                        return;

                    case AnalysisKind.Semantic:
                        _semantic = data;
                        return;

                    default:
                        Contract.Fail("Shouldn't reach here");
                        return;
                }
            }
        }
    }
}
