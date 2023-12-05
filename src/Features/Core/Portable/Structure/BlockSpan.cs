// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        /// Gets the optional span of the primary header of the code block represented by this tag. For example, in the
        /// following snippet of code:
        /// <code>
        ///     if (condition1)
        ///     {
        ///         //something;
        ///     }
        ///     else
        ///     {
        ///         // something else;
        ///     }
        /// </code>
        /// The primary span representing "else" statement block would be the same as the <see cref="TextSpan"/> of
        /// block span for the  "if" block. This allows structure visualizing features to provide more useful context
        /// when visualizing "else" structure blocks.
        /// </summary>
        public (TextSpan textSpan, TextSpan hintSpan)? PrimarySpans { get; }

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

        /// <summary>
        /// A string defined from <see cref="BlockTypes"/>.
        /// </summary>
        public string Type { get; }

        public BlockSpan(
            string type, bool isCollapsible, TextSpan textSpan, string bannerText = Ellipses, bool autoCollapse = false, bool isDefaultCollapsed = false)
            : this(type, isCollapsible, textSpan, textSpan, primarySpans: null, bannerText, autoCollapse, isDefaultCollapsed)
        {
        }

        public BlockSpan(
            string type,
            bool isCollapsible,
            TextSpan textSpan,
            TextSpan hintSpan,
            (TextSpan textSpan, TextSpan hintSpan)? primarySpans = null,
            string bannerText = Ellipses,
            bool autoCollapse = false,
            bool isDefaultCollapsed = false)
        {
            TextSpan = textSpan;
            HintSpan = hintSpan;
            PrimarySpans = primarySpans;
            BannerText = bannerText;
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
            Optional<(TextSpan textSpan, TextSpan hintSpan)> primarySpans = default,
            Optional<string> type = default,
            Optional<string> bannerText = default,
            Optional<bool> autoCollapse = default,
            Optional<bool> isDefaultCollapsed = default)
        {
            var newIsCollapsible = isCollapsible.HasValue ? isCollapsible.Value : IsCollapsible;
            var newTextSpan = textSpan.HasValue ? textSpan.Value : TextSpan;
            var newHintSpan = hintSpan.HasValue ? hintSpan.Value : HintSpan;
            var newPrimarySpans = primarySpans.HasValue ? primarySpans.Value : PrimarySpans;
            var newType = type.HasValue ? type.Value : Type;
            var newBannerText = bannerText.HasValue ? bannerText.Value : BannerText;
            var newAutoCollapse = autoCollapse.HasValue ? autoCollapse.Value : AutoCollapse;
            var newIsDefaultCollapsed = isDefaultCollapsed.HasValue ? isDefaultCollapsed.Value : IsDefaultCollapsed;

            return new BlockSpan(
                newType, newIsCollapsible, newTextSpan, newHintSpan, newPrimarySpans, newBannerText, newAutoCollapse, newIsDefaultCollapsed);
        }
    }
}
