// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal sealed class RecommendedKeyword
    {
        public Glyph Glyph { get; }
        public string Keyword { get; }
        public Func<CancellationToken, ImmutableArray<SymbolDisplayPart>> DescriptionFactory { get; }
        public bool IsIntrinsic { get; }
        public bool ShouldFormatOnCommit { get; }
        public int MatchPriority { get; }

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

        public RecommendedKeyword(
            string keyword,
             Glyph glyph,
              Func<CancellationToken, ImmutableArray<SymbolDisplayPart>> descriptionFactory,
              bool isIntrinsic = false,
              bool shouldFormatOnCommit = false,
              int? matchPriority = null)
        {
            Keyword = keyword;
            Glyph = glyph;
            DescriptionFactory = descriptionFactory;
            IsIntrinsic = isIntrinsic;
            ShouldFormatOnCommit = shouldFormatOnCommit;
            MatchPriority = matchPriority ?? Completion.MatchPriority.Default;
        }
    }
}
