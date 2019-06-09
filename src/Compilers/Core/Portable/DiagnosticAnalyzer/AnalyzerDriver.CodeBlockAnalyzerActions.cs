// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal partial class AnalyzerDriver<TLanguageKindEnum> : AnalyzerDriver where TLanguageKindEnum : struct
    {
        [StructLayout(LayoutKind.Auto)]
        private struct CodeBlockAnalyzerActions
        {
            public DiagnosticAnalyzer Analyzer;
            public ImmutableArray<CodeBlockStartAnalyzerAction<TLanguageKindEnum>> CodeBlockStartActions;
            public ImmutableArray<CodeBlockAnalyzerAction> CodeBlockActions;
            public ImmutableArray<CodeBlockAnalyzerAction> CodeBlockEndActions;
            public ImmutableArray<OperationBlockStartAnalyzerAction> OperationBlockStartActions;
            public ImmutableArray<OperationBlockAnalyzerAction> OperationBlockActions;
            public ImmutableArray<OperationBlockAnalyzerAction> OperationBlockEndActions;
        }
    }
}
