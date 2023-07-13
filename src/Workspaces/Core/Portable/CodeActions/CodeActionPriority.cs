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
    /// Medium priority code action.
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High priority code action. Note: High priority is simply a request on the part of a <see cref="CodeAction"/>.
    /// The core engine may automatically downgrade these items to <see cref="Default"/> priority.
    /// </summary>
    High = 3,

    /// <summary>
    /// Default priority for code actions.  Equivalent to <see cref="Medium"/>.
    /// </summary>
    Default = Medium,
}

#if false
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

    public static CodeActionPriority ConvertToInternalPriority(this CodeActionPriority priority)
    {
        priority = priority.Clamp();

        return priority switch
        {
            CodeActionPriority.Lowest => CodeActionPriority.Lowest,
            CodeActionPriority.Low => CodeActionPriority.Low,
            CodeActionPriority.Medium => CodeActionPriority.Medium,
            _ => throw ExceptionUtilities.UnexpectedValue(priority),
        };
    }
}
#endif
