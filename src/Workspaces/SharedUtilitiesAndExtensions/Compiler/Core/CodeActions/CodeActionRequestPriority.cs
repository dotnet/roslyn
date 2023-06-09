// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CodeActions
{
    /// <summary>
    /// Priority class that a particular <see cref="CodeRefactoringProvider"/> or <see cref="CodeFixProvider"/> should
    /// run at.  Providers are run in priority order, allowing the results of higher priority providers to be computed
    /// and shown to the user without having to wait on, or share computing resources with, lower priority providers.
    /// Providers should choose lower priority classes if they are either:
    /// <list type="number">
    /// <item>Very slow.  Slow providers will impede computing results for other providers in the same priority class.
    /// So running in a lower one means that fast providers can still get their results to users quickly.</item>
    /// <item>Less relevant.  Providers that commonly show available options, but those options are less likely to be
    /// taken, should run in lower priority groups.  This helps ensure their items are still there when the user wants
    /// them, but aren't as prominently shown.</item>
    /// </list>
    /// </summary>
    public enum CodeActionRequestPriority
    {
        /// <summary>
        /// Run the priority below <see cref="Normal"/> priority.  The provider may run slow, or its results may be
        /// commonly less relevant for the user.
        /// </summary>
        Low = 0,

        /// <summary>
        /// Run this provider at normal priority.   The provider will run in reasonable speeds and provide results that
        /// are commonly relevant to the user.
        /// </summary>
        Normal = 1,

        /// <summary>
        /// The default priority a provider should run at.  Currently <see cref="Normal"/>.
        /// </summary>
        Default = Normal,
    }

#pragma warning disable CA1200 // Avoid using cref tags with a prefix
    internal enum CodeActionRequestPriorityInternal
    {
        /// <summary>
        /// No priority specified, all refactoring, code fixes, and analyzers should be run.  This is equivalent
        /// to <see cref="Lowest"/>, <see cref="Low"/>, <see cref="Normal"/> and <see cref="High"/> combined.
        /// </summary>
        None = 0,

        /// <summary>
        /// Only lowest priority suppression and configuration fix providers should be run.  Specifically,
        /// <see cref="T:IConfigurationFixProvider"/> providers will be run.
        /// NOTE: This priority is reserved for suppression and configuration fix providers and should not be
        /// used by regular code fix providers and refactoring providers.
        /// </summary>
        Lowest = 1,

        /// <summary>
        /// Only low priority refactoring, code fix providers should be run.  Specifically,
        /// providers will be run when <see cref="T:CodeRefactoringProvider.RequestPriority"/> or
        /// <see cref="T:CodeFixProvider.RequestPriority"/> is <see cref="Low"/>.  <see cref="DiagnosticAnalyzer"/>s
        /// which can report at least one fixable diagnostic will be run.
        /// </summary>
        Low = 2,

        /// <summary>
        /// Only normal priority refactoring, code fix providers should be run.  Specifically,
        /// providers will be run when <see cref="T:CodeRefactoringProvider.RequestPriority"/> or
        /// <see cref="T:CodeFixProvider.RequestPriority"/> is <see cref="Normal"/>.  <see cref="DiagnosticAnalyzer"/>s
        /// which can report at least one fixable diagnostic will be run.
        /// </summary>
        Normal = 3,

        /// <summary>
        /// Only high priority refactoring, code fix providers should be run.  Specifically, providers will be run when
        /// <see cref="T:CodeRefactoringProvider.RequestPriority"/> or <see cref="T:CodeFixProvider.RequestPriority"/>
        /// is <see cref="High"/>. The <see cref="DiagnosticAnalyzerExtensions.IsCompilerAnalyzer"/> <see
        /// cref="DiagnosticAnalyzer"/> will be run.
        /// </summary>
        /// <remarks>Providers that return this should ensure that the appropriate <see
        /// cref="T:Microsoft.CodeAnalysis.CodeActions.CodeAction"/>s they return have a <see
        /// cref="T:CodeAction.Priority"/> of <see cref="T:CodeActionPriority.High"/>
        /// </remarks>
        High = 4,
    }
}
