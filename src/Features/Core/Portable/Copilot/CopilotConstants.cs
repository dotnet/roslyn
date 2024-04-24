// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Copilot;

internal static class CopilotConstants
{
    /// <summary>
    /// ID for the Copilot logo icon.
    /// </summary>
    public const int CopilotIconLogoId = 1;

    /// <summary>
    /// ID for the Copilot sparkle icon.
    /// </summary>
    public const int CopilotIconSparkleId = 2;

    /// <summary>
    /// ID for the Copilot blue sparkle icon.
    /// </summary>
    public const int CopilotIconSparkleBlueId = 3;

    /// <summary>
    /// GUID for Copilot icons.
    /// </summary>
    public static readonly Guid CopilotIconMonikerGuid = new Guid("{4515B9BD-70A1-45FA-9545-D4536417C596}");
}
