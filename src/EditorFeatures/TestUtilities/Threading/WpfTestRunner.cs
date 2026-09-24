// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Roslyn.Test.Utilities;

/// <summary>
/// Helper methods for tests which require <see cref="WpfFactAttribute"/> or <see cref="WpfTheoryAttribute"/>.
/// </summary>
public static class WpfTestRunner
{
    /// <summary>
    /// Asserts that the test is running on a <see cref="WpfFactAttribute"/> or <see cref="WpfTheoryAttribute"/>
    /// test method.
    /// </summary>
    internal static void RequireWpfFact(string reason)
    {
        if (TestExportJoinableTaskContext.GetEffectiveSynchronizationContext() is not DispatcherSynchronizationContext)
        {
            throw new InvalidOperationException($"This test requires {nameof(WpfFactAttribute)} because '{reason}' but is missing {nameof(WpfFactAttribute)}. Either the attribute should be changed, or the reason it needs an STA thread audited.");
        }
    }
}
