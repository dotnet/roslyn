// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeActions
{
    internal enum CodeActionProviderPriority
    {
        /// <summary>
        /// No priority specified, all refactoring, code fixes, and analyzers should be run.
        /// </summary>
        None = 0,
        /// <summary>
        /// Only normal priority refactoring, code fix providers should be run.  Specifically,
        /// providers will be run when <see cref="CodeRefactoringProvider.IsHighPriority"/> and
        /// <see cref="CodeFixProvider.IsHighPriority"/> are <see langword="false"/>.  <see cref="DiagnosticAnalyzer"/>s
        /// will be run except for <see cref="DiagnosticAnalyzerExtensions.IsCompilerAnalyzer"/>.
        /// </summary>
        Normal = 1,
        /// <summary>
        /// Only high priority refactoring, code fix providers should be run.  Specifically,
        /// providers will be run when <see cref="CodeRefactoringProvider.IsHighPriority"/> or
        /// <see cref="CodeFixProvider.IsHighPriority"/> is <see langword="true"/>.
        /// The <see cref="DiagnosticAnalyzerExtensions.IsCompilerAnalyzer"/> <see cref="DiagnosticAnalyzer"/>
        /// will be run.
        /// </summary>
        High = 2,
    }
}
