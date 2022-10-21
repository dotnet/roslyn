// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Structure
{
    internal readonly struct FSharpBlockSpan
    {
        private const string Ellipses = "...";

        /// <summary>
        /// Whether or not this span can be collapsed.
        /// </summary>
        public bool IsCollapsible { get; }

        /// <summary>
        /// The span of text to collapse.
        /// </summary>
        public TextSpan TextSpan { get; }

        /// <summary>
        /// The span of text to display in the hint on mouse hover.
        /// </summary>
        public TextSpan HintSpan { get; }

        /// <summary>
        /// The text to display inside the collapsed region.
        /// </summary>
        public string BannerText { get; }

        /// <summary>
        /// Whether or not this region should be automatically collapsed when the 'Collapse to Definitions' command is invoked.
        /// </summary>
        public bool AutoCollapse { get; }

        /// <summary>
        /// Whether this region should be collapsed by default when a file is opened the first time.
        /// </summary>
        public bool IsDefaultCollapsed { get; }

        public string Type { get; }

#pragma warning disable RS0059 // Do not add multiple public overloads with optional parameters
        public FSharpBlockSpan(
#pragma warning restore RS0059 // Do not add multiple public overloads with optional parameters
            string type, bool isCollapsible, TextSpan textSpan, string bannerText = Ellipses, bool autoCollapse = false, bool isDefaultCollapsed = false)
            : this(type, isCollapsible, textSpan, textSpan, bannerText, autoCollapse, isDefaultCollapsed)
        {
        }

#pragma warning disable RS0059 // Do not add multiple public overloads with optional parameters
        public FSharpBlockSpan(
#pragma warning restore RS0059 // Do not add multiple public overloads with optional parameters
            string type, bool isCollapsible, TextSpan textSpan, TextSpan hintSpan, string bannerText = Ellipses, bool autoCollapse = false, bool isDefaultCollapsed = false)
        {
            TextSpan = textSpan;
            BannerText = bannerText;
            HintSpan = hintSpan;
            AutoCollapse = autoCollapse;
            IsDefaultCollapsed = isDefaultCollapsed;
            IsCollapsible = isCollapsible;
            Type = type;
        }

        public override string ToString()
        {
            return this.TextSpan != this.HintSpan
                ? $"{{Span={TextSpan}, HintSpan={HintSpan}, BannerText=\"{BannerText}\", AutoCollapse={AutoCollapse}, IsDefaultCollapsed={IsDefaultCollapsed}}}"
                : $"{{Span={TextSpan}, BannerText=\"{BannerText}\", AutoCollapse={AutoCollapse}, IsDefaultCollapsed={IsDefaultCollapsed}}}";
        }
    }
}
