// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Roslyn.VisualStudio.NewIntegrationTests.InProcess;

internal enum HotReloadAction
{
    None,
    Cancel,
    Edit,
    Disable,
    Revert,
    Ignore,
    Restart,
    Stop,
}
