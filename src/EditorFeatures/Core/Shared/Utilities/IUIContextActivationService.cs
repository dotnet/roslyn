// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities;

internal interface IUIContextActivationService
{
    /// <summary>
    /// Executes the specified action when the UIContext first becomes active, or immediately if it is already active
    /// </summary>
    void ExecuteWhenActivated(Guid uiContext, Action action);
}
