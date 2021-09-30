// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeActions
{
#pragma warning disable CA1200 // Avoid using cref tags with a prefix
    internal enum CodeActionRequestPriority
    {
        /// <summary>
        /// No priority specified, all refactoring, code fixes, and analyzers should be run.  This is equivalent
        /// to <see cref="Low"/>, <see cref="Normal"/> and <see cref="High"/> combined.
        /// </summary>
        None = 0,

        /// <summary>
        /// Only low priority suppression and configuration fix providers should be run.  Specifically,
        /// <see cref="T:IConfigurationFixProvider"/> providers will be run.
        /// </summary>
        Low = 1,

        /// <summary>
        /// Only normal priority refactoring, code fix providers should be run.  Specifically,
        /// providers will be run when <see cref="T:CodeRefactoringProvider.RequestPriority"/> or
        /// <see cref="T:CodeFixProvider.RequestPriority"/> is <see cref="Normal"/>.  <see cref="DiagnosticAnalyzer"/>s
        /// which can report at least one fixable diagnostic will be run.
        /// </summary>
        Normal = 2,

        /// <summary>
        /// Only high priority refactoring, code fix providers should be run.  Specifically,
        /// providers will be run when <see cref="T:CodeRefactoringProvider.RequestPriority"/> or
        /// <see cref="T:CodeFixProvider.RequestPriority"/> is <see cref="Normal"/>.
        /// The <see cref="DiagnosticAnalyzerExtensions.IsCompilerAnalyzer"/> <see cref="DiagnosticAnalyzer"/>
        /// will be run.
        /// </summary>
        High = 3,
    }
}
