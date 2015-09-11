// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [Flags]
    internal enum DiagnosticAnalyzerCategory
    {
        /// <summary>
        /// Invalid value, analyzer must support at least one or more of the subsequent analysis categories.
        /// </summary>
        None = 0,
        
        /// <summary>
        /// Analyzer reports syntax diagnostics (i.e. registers a SyntaxTree action).
        /// </summary>
        SyntaxAnalysis = 0x0001,

        /// <summary>
        /// Analyzer reports semantic diagnostics and also supports incremental span based method body analysis.
        /// An analyzer can support incremental method body analysis if edits within a method body only affect the diagnostics reported by the analyzer on the edited method body.
        /// </summary>
        SemanticSpanAnalysis = 0x0010,

        /// <summary>
        /// Analyzer reports semantic diagnostics but doesn't support incremental span based method body analysis.
        /// It needs to re-analyze the whole document for reporting semantic diagnostics even for method body editing scenarios.
        /// </summary>
        SemanticDocumentAnalysis = 0x0100,

        /// <summary>
        /// Analyzer reports project diagnostics (i.e. registers a Compilation action and/or Compilation end action diagnostics).
        /// </summary>
        ProjectAnalysis = 0x1000
    }
}
