// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal sealed class RecommendedKeyword(
        string keyword,
         Glyph glyph,
          Func<CancellationToken, ImmutableArray<SymbolDisplayPart>> descriptionFactory,
          bool isIntrinsic = false,
          bool shouldFormatOnCommit = false,
          int? matchPriority = null)
    {
        public Glyph Glyph { get; } = glyph;
        public string Keyword { get; } = keyword;
        public Func<CancellationToken, ImmutableArray<SymbolDisplayPart>> DescriptionFactory { get; } = descriptionFactory;
        public bool IsIntrinsic { get; } = isIntrinsic;
        public bool ShouldFormatOnCommit { get; } = shouldFormatOnCommit;
        public int MatchPriority { get; } = matchPriority ?? Completion.MatchPriority.Default;

        public RecommendedKeyword(string keyword, string toolTip = "", Glyph glyph = Glyph.Keyword, bool isIntrinsic = false, bool shouldFormatOnCommit = false, int? matchPriority = null)
            : this(keyword, glyph, _ => CreateDisplayParts(keyword, toolTip), isIntrinsic, shouldFormatOnCommit, matchPriority)
        {
        }

        internal static ImmutableArray<SymbolDisplayPart> CreateDisplayParts(string keyword, string toolTip)
        {
            var textContentBuilder = new System.Collections.Generic.List<SymbolDisplayPart>();
            textContentBuilder.AddText(string.Format(FeaturesResources._0_Keyword, keyword));

            if (!string.IsNullOrEmpty(toolTip))
            {
                textContentBuilder.AddLineBreak();
                textContentBuilder.AddText(toolTip);
            }

            return textContentBuilder.ToImmutableArray();
        }
    }
}
