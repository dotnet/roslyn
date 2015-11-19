// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static partial class SymbolDisplayPartExtensions
    {
        public static Run ToRun(this SymbolDisplayPart part, IClassificationFormatMap formatMap, ClassificationTypeMap typeMap)
        {
            var text = part.ToVisibleDisplayString(includeLeftToRightMarker: true);

            var run = new Run(text);

            var format = formatMap.GetTextProperties(typeMap.GetClassificationType(part.Kind.ToClassificationTypeName()));
            run.SetTextProperties(format);

            return run;
        }

        public static TextBlock ToTextBlock(this ImmutableArray<SymbolDisplayPart> parts, ClassificationTypeMap typeMap)
        {
            return parts.AsEnumerable().ToTextBlock(typeMap);
        }

        public static TextBlock ToTextBlock(this IEnumerable<SymbolDisplayPart> parts, ClassificationTypeMap typeMap)
        {
            var result = new TextBlock() { TextWrapping = TextWrapping.Wrap };

            var formatMap = typeMap.ClassificationFormatMapService.GetClassificationFormatMap("tooltip");
            result.SetDefaultTextProperties(formatMap);

            foreach (var part in parts)
            {
                result.Inlines.Add(part.ToRun(formatMap, typeMap));
            }

            return result;
        }

        public static IList<ClassificationSpan> ToClassificationSpans(
            this IEnumerable<SymbolDisplayPart> parts,
            ITextSnapshot textSnapshot,
            ClassificationTypeMap typeMap)
        {
            var result = new List<ClassificationSpan>();

            var index = 0;
            foreach (var part in parts)
            {
                var text = part.ToString();
                result.Add(new ClassificationSpan(
                    new SnapshotSpan(textSnapshot, new Microsoft.VisualStudio.Text.Span(index, text.Length)),
                    typeMap.GetClassificationType(part.Kind.ToClassificationTypeName())));

                index += text.Length;
            }

            return result;
        }
    }
}
