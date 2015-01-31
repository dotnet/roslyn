// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class SymbolDisplayPartExtensions
    {
        public static IEnumerable<ClassifiedSpan> ToClassifiedSpans(this ImmutableArray<SymbolDisplayPart> parts)
        {
            return parts.AsEnumerable().ToClassifiedSpans();
        }

        public static IEnumerable<ClassifiedSpan> ToClassifiedSpans(this IEnumerable<SymbolDisplayPart> parts)
        {
            var index = 0;
            foreach (var part in parts)
            {
                var text = part.ToString();
                var classificationTypeName = part.Kind.ToClassificationTypeName();

                yield return new ClassifiedSpan(new TextSpan(index, text.Length), classificationTypeName);
                index += text.Length;
            }
        }
    }
}
