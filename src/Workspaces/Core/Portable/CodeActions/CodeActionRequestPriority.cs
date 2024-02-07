// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeActions;

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
    /// Only lowest priority suppression and configuration fix providers should be run.  Specifically, <see
    /// cref="IConfigurationFixProvider"/> providers will be run. NOTE: This priority is reserved for suppression and
    /// configuration fix providers and should not be used by regular code fix providers and refactoring providers.
    /// </summary>
    Lowest = 1,

    /// <summary>
    /// Run the priority below <see cref="Default"/> priority.  The provider may run slow, or its results may be
    /// commonly less relevant for the user.
    /// </summary>
    Low = 2,

    /// <summary>
    /// Run this provider at default priority.  The provider will run in reasonable speeds and provide results that are
    /// commonly relevant to the user.
    /// </summary>
    Default = 3,

    /// <summary>
    /// Run this provider at high priority. Note: High priority is simply a request on the part of a provider. The core
    /// engine may automatically downgrade these items to <see cref="Default"/> priority.
    /// </summary>
    High = 4,
}

internal static class CodeActionRequestPriorityExtensions
{
    /// <summary>
    /// Clamps the value of <paramref name="priority"/> (which could be any integer) to the legal range of values
    /// present in <see cref="CodeActionRequestPriority"/>.
    /// </summary>
    public static CodeActionRequestPriority Clamp(this CodeActionRequestPriority priority, ImmutableArray<string> customTags)
    {
        // Note: we intentionally clamp things lower than 'Low' (including 'Lowest') priorities to 'Low'.  The 'Lowest'
        // value is only for use by specialized suppression/configuration providers.  Any values returned by an actual
        // regular provider (either 1st or 3rd party) should still only be between Low and High.
        if (priority < CodeActionRequestPriority.Low)
            priority = CodeActionRequestPriority.Low;

        if (priority > CodeActionRequestPriority.High)
            priority = CodeActionRequestPriority.High;

        if (priority == CodeActionRequestPriority.High && !customTags.Contains(CodeAction.CanBeHighPriorityTag))
            priority = CodeActionRequestPriority.Default;

        return priority;
    }

    public static int GetPriorityInt(this CodeActionRequestPriority? priority)
        => priority switch
        {
            null => 0,
            { } nonNull => (int)nonNull
        };
}
