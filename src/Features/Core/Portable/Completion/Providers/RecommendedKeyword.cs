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
        public bool ShouldPreselect { get; }

        public RecommendedKeyword(string keyword, string toolTip = "", Glyph glyph = Glyph.Keyword, bool isIntrinsic = false, bool shouldFormatOnCommit = false, bool shouldPreselect = false)
            : this(keyword, glyph, _ => CreateDisplayParts(keyword, toolTip), isIntrinsic, shouldFormatOnCommit, shouldPreselect)
        {
        }

        internal static ImmutableArray<SymbolDisplayPart> CreateDisplayParts(string keyword, string toolTip)
        {
            var textContentBuilder = new System.Collections.Generic.List<SymbolDisplayPart>();
            textContentBuilder.AddText(string.Format(FeaturesResources.Keyword, keyword));

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
              bool shouldPreselect = false)
        {
            this.Keyword = keyword;
            this.Glyph = glyph;
            this.DescriptionFactory = descriptionFactory;
            this.IsIntrinsic = isIntrinsic;
            this.ShouldFormatOnCommit = shouldFormatOnCommit;
            this.ShouldPreselect = shouldPreselect;
        }
    }
}
