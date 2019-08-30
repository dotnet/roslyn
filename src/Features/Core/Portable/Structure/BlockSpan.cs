// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Structure
{
    internal readonly struct BlockSpan
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

        public BlockSpan(
            string type, bool isCollapsible, TextSpan textSpan, string bannerText = Ellipses, bool autoCollapse = false, bool isDefaultCollapsed = false)
            : this(type, isCollapsible, textSpan, textSpan, bannerText, autoCollapse, isDefaultCollapsed)
        {
        }

        public BlockSpan(
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
            return TextSpan != HintSpan
                ? $"{{Span={TextSpan}, HintSpan={HintSpan}, BannerText=\"{BannerText}\", AutoCollapse={AutoCollapse}, IsDefaultCollapsed={IsDefaultCollapsed}}}"
                : $"{{Span={TextSpan}, BannerText=\"{BannerText}\", AutoCollapse={AutoCollapse}, IsDefaultCollapsed={IsDefaultCollapsed}}}";
        }

        internal BlockSpan WithType(string type)
            => With(type: type);

        internal BlockSpan WithIsCollapsible(bool isCollapsible)
            => With(isCollapsible: isCollapsible);

        internal BlockSpan With(
            Optional<bool> isCollapsible = default,
            Optional<TextSpan> textSpan = default,
            Optional<TextSpan> hintSpan = default,
            Optional<string> type = default,
            Optional<string> bannerText = default,
            Optional<bool> autoCollapse = default,
            Optional<bool> isDefaultCollapsed = default)
        {
            var newIsCollapsible = isCollapsible.HasValue ? isCollapsible.Value : IsCollapsible;
            var newTextSpan = textSpan.HasValue ? textSpan.Value : TextSpan;
            var newHintSpan = hintSpan.HasValue ? hintSpan.Value : HintSpan;
            var newType = type.HasValue ? type.Value : Type;
            var newBannerText = bannerText.HasValue ? bannerText.Value : BannerText;
            var newAutoCollapse = autoCollapse.HasValue ? autoCollapse.Value : AutoCollapse;
            var newIsDefaultCollapsed = isDefaultCollapsed.HasValue ? isDefaultCollapsed.Value : IsDefaultCollapsed;

            return new BlockSpan(
                newType, newIsCollapsible, newTextSpan, newHintSpan, newBannerText, newAutoCollapse, newIsDefaultCollapsed);
        }
    }
}
