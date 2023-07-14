// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeActions;

#pragma warning disable CA1200 // Avoid using cref tags with a prefix

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
    /// No priority specified, all refactoring, code fixes, and analyzers should be run.  This is equivalent
    /// to <see cref="Lowest"/>, <see cref="Low"/>, <see cref="Medium"/> and <see cref="High"/> combined.
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
    /// Run the priority below <see cref="Medium"/> priority.  The provider may run slow, or its results may be
    /// commonly less relevant for the user.
    /// </summary>
    Low = 2,

    /// <summary>
    /// Run this provider at default priority.  The provider will run in reasonable speeds and provide results that are
    /// commonly relevant to the user.
    /// </summary>
    Medium = 3,

    /// <summary>
    /// Run this provider at high priority. Note: High priority is simply a request on the part of a provider. The core
    /// engine may automatically downgrade these items to <see cref="Default"/> priority.
    /// </summary>
    High = 4,

    /// <summary>
    /// Default provider priority.  Equivalent to <see cref="Medium"/>.
    /// </summary>
    Default = Medium,
}

internal static class CodeActionRequestPriorityExtensions
{
    /// <summary>
    /// Special tag that indicates that it's this is a privileged code action that is allowed to use the <see
    /// cref="CodeActionRequestPriority.High"/> priority class.
    /// </summary>
    public static readonly string CanBeHighPriorityTag = Guid.NewGuid().ToString();

    /// <summary>
    /// Clamps the value of <paramref name="priority"/> (which could be any integer) to the legal range of values
    /// present in <see cref="CodeActionRequestPriority"/>.
    /// </summary>
    public static CodeActionRequestPriority Clamp(this CodeActionRequestPriority priority, ImmutableArray<string> customTags)
    {
        if (priority < CodeActionRequestPriority.Low)
            priority = CodeActionRequestPriority.Low;

        if (priority > CodeActionRequestPriority.High)
            priority = CodeActionRequestPriority.Default;

        if (priority == CodeActionRequestPriority.High && !customTags.Contains(CanBeHighPriorityTag))
            priority = CodeActionRequestPriority.Default;

        return priority;
    }

    //public static CodeActionRequestPriority ConvertToInternalPriority(this CodeActionRequestPriority priority)
    //{
    //    priority = priority.Clamp();

    //    return priority switch
    //    {
    //        CodeActionRequestPriority.Low => CodeActionRequestPriority.Low,
    //        CodeActionRequestPriority.Medium => CodeActionRequestPriority.Normal,
    //        _ => throw ExceptionUtilities.UnexpectedValue(priority),
    //    };
    //}
}
