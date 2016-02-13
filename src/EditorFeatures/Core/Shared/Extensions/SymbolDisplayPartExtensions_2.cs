// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static partial class SymbolDisplayPartExtensions
    {
        private const string LeftToRightMarkerPrefix = "\u200e";

        public static string ToVisibleDisplayString(this SymbolDisplayPart part, bool includeLeftToRightMarker)
        {
            var text = part.ToString();

            if (includeLeftToRightMarker)
            {
                var classificationTypeName = part.Kind.ToClassificationTypeName();
                if (classificationTypeName == ClassificationTypeNames.Punctuation ||
                    classificationTypeName == ClassificationTypeNames.WhiteSpace)
                {
                    text = LeftToRightMarkerPrefix + text;
                }
            }

            return text;
        }

        public static string ToVisibleDisplayString(this IEnumerable<SymbolDisplayPart> parts, bool includeLeftToRightMarker)
        {
            return string.Join(string.Empty, parts.Select(p => p.ToVisibleDisplayString(includeLeftToRightMarker)));
        }
    }
}
