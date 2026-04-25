// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.VisualStudio.Razor;

internal interface IProjectCapabilityResolver
{
    /// <summary>
    /// Determines whether the project associated with the specified document has the given <paramref name="capability"/>.
    /// </summary>
    CapabilityCheckResult CheckCapability(string capability, string documentFilePath);
}
