// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api;

internal readonly struct VSTypeScriptBlockSpan(
    string? type, bool isCollapsible, TextSpan textSpan, TextSpan hintSpan, string bannerText = VSTypeScriptBlockSpan.Ellipses, bool autoCollapse = false, bool isDefaultCollapsed = false)
{
    private const string Ellipses = "...";

    public bool IsCollapsible { get; } = isCollapsible;
    public TextSpan TextSpan { get; } = textSpan;
    public TextSpan HintSpan { get; } = hintSpan;
    public string BannerText { get; } = bannerText;
    public bool AutoCollapse { get; } = autoCollapse;
    public bool IsDefaultCollapsed { get; } = isDefaultCollapsed;
    public string? Type { get; } = type;
}
