// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.EditAndContinue;

/// <summary>
/// Retrieves the <see cref="Solution"/> snapshot that corresponds to the current state of the debuggee.
/// This snapshot contains changes successfully applied during EnC/Hot Reload.
/// </summary>
/// <remarks>
/// This is temporarily available to in-proc XAML External Access APIs and should be revisited once XAML moves to OOP LSP.
/// </remarks>
internal interface IEditAndContinueSolutionProvider
{
    event Action<Solution> SolutionCommitted;
}
