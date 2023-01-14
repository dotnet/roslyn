// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.VSTypeScript.Api
{
    internal readonly struct VSTypeScriptBlockSpan
    {
        private const string Ellipses = "...";

        public bool IsCollapsible { get; }
        public TextSpan TextSpan { get; }
        public TextSpan HintSpan { get; }
        public string BannerText { get; }
        public bool AutoCollapse { get; }
        public bool IsDefaultCollapsed { get; }
        public string? Type { get; }

        public VSTypeScriptBlockSpan(
            string? type, bool isCollapsible, TextSpan textSpan, TextSpan hintSpan, string bannerText = Ellipses, bool autoCollapse = false, bool isDefaultCollapsed = false)
        {
            TextSpan = textSpan;
            BannerText = bannerText;
            HintSpan = hintSpan;
            AutoCollapse = autoCollapse;
            IsDefaultCollapsed = isDefaultCollapsed;
            IsCollapsible = isCollapsible;
            Type = type;
        }
    }
}
