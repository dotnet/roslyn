// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal partial class AnalyzerDriver<TLanguageKindEnum> : AnalyzerDriver where TLanguageKindEnum : struct
    {
        [StructLayout(LayoutKind.Auto)]
        private struct ExecutableCodeBlockAnalyzerActions
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
