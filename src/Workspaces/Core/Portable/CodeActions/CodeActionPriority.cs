// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.CodeActions;

/// <summary>
/// Priority of a particular code action produced by either a <see cref="CodeRefactoringProvider"/> or a <see
/// cref="CodeFixProvider"/>.  Code actions use priorities to group themselves, with lower priority actions showing
/// up after higher priority ones.  Providers should put less relevant code actions into lower priority buckets to
/// have them appear later in the UI, allowing the user to get to important code actions more quickly.
/// </summary>
public enum CodeActionPriority
{
    /// <summary>
    /// Lowest priority code actions.  Will show up after <see cref="Low"/> priority items.
    /// </summary>
    Lowest = 0,

    /// <summary>
    /// Low priority code action.  Will show up after <see cref="Default"/> priority items.
    /// </summary>
    Low = 1,

    /// <summary>
    /// Medium priority code action.
    /// </summary>
    Default = 2,

    /// <summary>
    /// High priority code action. Note: High priority is simply a request on the part of a <see cref="CodeAction"/>.
    /// The core engine may automatically downgrade these items to <see cref="Default"/> priority.
    /// </summary>
    // <remarks>
    // If <see cref="CodeActionPriority.High"/> is used, the analyzer that specifies that value should implement and
    // return true for <see cref="IBuiltInAnalyzer.IsHighPriority"/>, and <see cref="CodeActionRequestPriority.High> for
    // <see cref="T:Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider.RequestPriority"/> and <see
    // cref="T:Microsoft.CodeAnalysis.CodeRefactorings.CodeRefactoringProvider.RequestPriority"/>. This will ensure that
    // the analysis engine runs the analzyers and providers that will produce those actions first, thus allowing those
    // actions to be computed and displayed prior to running all other providers.
    // </remarks>
    High = 3,
}
