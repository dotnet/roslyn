﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis;

internal record struct WorkspaceEventOptions(bool RequiresMainThread)
{
    public static readonly WorkspaceEventOptions DefaultOptions = new(RequiresMainThread: false);
    public static readonly WorkspaceEventOptions RequiresMainThreadOptions = new(RequiresMainThread: true);
}
