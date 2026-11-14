// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using System.Windows.Threading;

namespace Roslyn.Test.Utilities;

/// <summary>
/// Helper methods for tests which require <see cref="WpfFactAttribute"/> or <see cref="WpfTheoryAttribute"/>.
/// </summary>
public static class WpfTestRunner
{
#pragma warning disable IDE0052 // Remove unread private members.  Can be used for debugging purposes.
    private static string s_wpfFactRequirementReason;
#pragma warning restore IDE0052 // Remove unread private members

    /// <summary>
    /// Clears the reason recorded by <see cref="RequireWpfFact"/>, ensuring that part of the next test actually
    /// needs <see cref="WpfFactAttribute"/>.
    /// </summary>
    internal static void ResetWpfFactRequirementReason()
        => s_wpfFactRequirementReason = null;

    /// <summary>
    /// Asserts that the test is running on a <see cref="WpfFactAttribute"/> or <see cref="WpfTheoryAttribute"/>
    /// test method, and records the reason for requiring the use of an STA thread.
    /// </summary>
    internal static void RequireWpfFact(string reason)
    {
        if (SynchronizationContext.Current is not DispatcherSynchronizationContext)
        {
            throw new InvalidOperationException($"This test requires {nameof(WpfFactAttribute)} because '{reason}' but is missing {nameof(WpfFactAttribute)}. Either the attribute should be changed, or the reason it needs an STA thread audited.");
        }

        s_wpfFactRequirementReason = reason;
    }
}
