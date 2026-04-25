// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.VisualStudio.Razor;

internal interface IProjectCapabilityListener
{
    void OnProjectCapabilityMatched(string projectFilePath, string capability, bool isMatch);
}
