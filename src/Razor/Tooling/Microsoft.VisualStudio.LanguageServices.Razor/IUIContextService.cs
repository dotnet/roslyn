// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.VisualStudio.Razor;

internal interface IUIContextService
{
    bool IsActive(Guid contextGuid);
}
