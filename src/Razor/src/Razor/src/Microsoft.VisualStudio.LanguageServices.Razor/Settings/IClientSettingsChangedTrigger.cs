// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis.Razor.Workspaces.Settings;

namespace Microsoft.VisualStudio.Razor.Settings;

internal interface IClientSettingsChangedTrigger
{
    void Initialize(IClientSettingsManager editorSettingsManager);
}
