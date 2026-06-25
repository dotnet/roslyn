// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Windows.Threading;

namespace Roslyn.Test.Utilities;

/// <summary>
/// Provides static helpers for WPF test execution context.
/// The actual STA dispatch is handled by <see cref="WpfTestCaseRunner"/>.
/// </summary>
public static class WpfTestRunner
{
#pragma warning disable IDE0052 // Remove unread private members — useful for debugging deadlocks
    internal static string s_wpfFactRequirementReason;
#pragma warning restore IDE0052

    /// <summary>
    /// Asserts that the current test is running on a WPF STA thread, and records why.
    /// </summary>
    internal static void RequireWpfFact(string reason)
    {
        if (TestExportJoinableTaskContext.GetEffectiveSynchronizationContext() is not DispatcherSynchronizationContext)
        {
            throw new InvalidOperationException(
                $"This test requires {nameof(WpfFactAttribute)} because '{reason}' but is missing " +
                $"{nameof(WpfFactAttribute)}. Either the attribute should be changed, or the reason " +
                $"it needs an STA thread audited.");
        }

        s_wpfFactRequirementReason = reason;
    }
}
