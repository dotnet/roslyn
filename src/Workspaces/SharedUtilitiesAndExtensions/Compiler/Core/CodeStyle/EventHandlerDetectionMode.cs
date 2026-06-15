// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CodeStyle;

/// <summary>
/// Controls how the <c>Make method async</c> code fix detects event-handler methods when deciding
/// whether to prefer <c>async void</c> over <c>async Task</c>.
/// </summary>
internal enum EventHandlerDetectionMode
{
    /// <summary>
    /// Fast check: a method is treated as an event handler when its parameter list matches
    /// the conventional <c>(object sender, TEventArgs e)</c> shape, where
    /// <c>TEventArgs</c> inherits from <see cref="System.EventArgs"/>.
    /// </summary>
    Signature = 0,

    /// <summary>
    /// Slower check: in addition to the signature check, also verifies that the method is
    /// actually assigned to an event somewhere in the compilation.
    /// </summary>
    References = 1,

    /// <summary>
    /// Event-handler detection is disabled entirely. Both <c>async Task</c> and <c>async void</c>
    /// are always offered in the same order with the same titles.
    /// </summary>
    Off = 2,
}
