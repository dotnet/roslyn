// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Outlining
{
    internal class OutliningSpan
    {
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

        public OutliningSpan(TextSpan textSpan, TextSpan hintSpan, string bannerText, bool autoCollapse, bool isDefaultCollapsed = false)
        {
            TextSpan = textSpan;
            BannerText = bannerText;
            HintSpan = hintSpan;
            AutoCollapse = autoCollapse;
            IsDefaultCollapsed = isDefaultCollapsed;
        }

        public OutliningSpan(TextSpan textSpan, string bannerText, bool autoCollapse, bool isDefaultCollapsed = false)
            : this(textSpan, textSpan, bannerText, autoCollapse, isDefaultCollapsed)
        {
        }

        public override string ToString()
        {
            return this.TextSpan != this.HintSpan
                ? $"{{Span={TextSpan}, HintSpan={HintSpan}, BannerText=\"{BannerText}\", AutoCollapse={AutoCollapse}, IsDefaultCollapsed={IsDefaultCollapsed}}}"
                : $"{{Span={TextSpan}, BannerText=\"{BannerText}\", AutoCollapse={AutoCollapse}, IsDefaultCollapsed={IsDefaultCollapsed}}}";
        }
    }
}
