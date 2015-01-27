// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Outlining
{
    internal class OutliningSpan
    {
        /// <summary>
        /// The span of text to collapse.
        /// </summary>
        public TextSpan TextSpan { get; private set; }

        /// <summary>
        /// The span of text to display in the hint on mouse hover.
        /// </summary>
        public TextSpan HintSpan { get; private set; }

        /// <summary>
        /// The text to display inside the collapsed region.
        /// </summary>
        public string BannerText { get; private set; }

        /// <summary>
        /// Whether or not this region should be automatically collapsed when the 'Collapse to Definitions' command is invoked.
        /// </summary>
        public bool AutoCollapse { get; private set; }

        public OutliningSpan(TextSpan textSpan, TextSpan hintSpan, string bannerText, bool autoCollapse)
        {
            this.TextSpan = textSpan;
            this.BannerText = bannerText;
            this.HintSpan = hintSpan;
            this.AutoCollapse = autoCollapse;
        }

        public OutliningSpan(TextSpan textSpan, string bannerText, bool autoCollapse)
            : this(textSpan, textSpan, bannerText, autoCollapse)
        {
        }

        public override string ToString()
        {
            return this.TextSpan != this.HintSpan
                ? string.Format("{{Span={0}, HintSpan={1}, BannerText=\"{2}\", AutoCollapse={3}}}", TextSpan, HintSpan, BannerText, AutoCollapse)
                : string.Format("{{Span={0}, BannerText=\"{1}\", AutoCollapse={2}}}", TextSpan, BannerText, AutoCollapse);
        }
    }
}
