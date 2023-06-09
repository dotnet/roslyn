// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

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
    /// Low priority code action.  Will show up after <see cref="Medium"/> priority items.
    /// </summary>
    Low = 1,

    /// <summary>
    /// Default priority code action.
    /// </summary>
    Medium = 2,
}

internal static class CodeActionPriorityExtensions
{
    /// <summary>
    /// Clamps the value of <paramref name="priority"/> (which could be any integer) to the legal range of values
    /// present in <see cref="CodeActionPriority"/>.
    /// </summary>
    private static CodeActionPriority Clamp(this CodeActionPriority priority)
    {
        if (priority < CodeActionPriority.Lowest)
            priority = CodeActionPriority.Lowest;

        if (priority > CodeActionPriority.Medium)
            priority = CodeActionPriority.Medium;

        return priority;
    }

    public static CodeActionPriorityInternal ConvertToInternalPriority(this CodeActionPriority priority)
    {
        priority = priority.Clamp();

        return priority switch
        {
            CodeActionPriority.Lowest => CodeActionPriorityInternal.Lowest,
            CodeActionPriority.Low => CodeActionPriorityInternal.Low,
            CodeActionPriority.Medium => CodeActionPriorityInternal.Medium,
            _ => throw ExceptionUtilities.UnexpectedValue(priority),
        };
    }
}

#pragma warning disable CA1200 // Avoid using cref tags with a prefix
/// <summary>
/// Internal priority used to bluntly place items in a light bulb in strict orderings.  Priorities take
/// the highest precedence when ordering items so that we can ensure very important items get top prominence,
/// and low priority items do not.
/// </summary>
/// <remarks>
/// If <see cref="CodeActionPriorityInternal.High"/> is used, the feature that specifies that value should 
/// implement and return <see cref="CodeActionRequestPriorityInternal.High"/> for <see cref="IBuiltInAnalyzer.RequestPriority"/>,
/// <see cref="T:Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider.RequestPriority"/> and
/// <see cref="T:Microsoft.CodeAnalysis.CodeRefactorings.CodeRefactoringProvider.RequestPriority"/>. This
/// will ensure that the analysis engine runs the providers that will produce those actions first,
/// thus allowing those actions to be computed and displayed prior to running all other providers.
/// </remarks>
internal enum CodeActionPriorityInternal
{
    Lowest = 0,
    Low = 1,
    Medium = 2,
    High = 3,

    Default = Medium
}
