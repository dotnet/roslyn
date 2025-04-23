// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Language.Suggestions;

namespace Microsoft.CodeAnalysis.Editor.Copilot;

internal static class CopilotConstants
{
    public const int CopilotIconLogoId = 1;
    public const int CopilotIconSparkleId = 2;
    public const int CopilotIconSparkleBlueId = 3;

    /// <summary>
    /// This flag is used to indicate that a thinking state tip should be shown.
    /// This will eventually be defined in Microsoft.VisualStudio.Language.Suggestions.TipStyle once the behavior is finalized.
    /// </summary>
    // Copied from https://devdiv.visualstudio.com/DevDiv/_git/IntelliCode-VS?path=%2Fsrc%2FVSIX%2FIntelliCode.VSIX%2FSuggestionService%2FImplementation%2FSuggestionSession.cs&amp
    public const TipStyle ShowThinkingStateTipStyle = (TipStyle)0x20;

    public static readonly Guid CopilotIconMonikerGuid = new("{4515B9BD-70A1-45FA-9545-D4536417C596}");
    public static readonly Guid CopilotQuotaExceededGuid = new("39B0DEDE-D931-4A92-9AA2-3447BC4998DC");
}
