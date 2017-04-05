// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static partial class ClassificationExtensions
    {
        public static Run ToRun(this ClassifiedText part, IClassificationFormatMap formatMap, ClassificationTypeMap typeMap)
        {
            var text = part.Text;

            var run = new Run(text);

            var format = formatMap.GetTextProperties(typeMap.GetClassificationType(
                part.ClassificationType));
            run.SetTextProperties(format);

            return run;
        }

        public static TextBlock ToTextBlock(this TaggedText part, ClassificationTypeMap typeMap)
        {
            return SpecializedCollections.SingletonEnumerable(part).ToTextBlock(typeMap);
        }

        public static IList<Inline> ToInlines(
            this IEnumerable<TaggedText> parts,
            ClassificationTypeMap typeMap,
            string classificationFormatMap = null)
        {
            var classifiedTexts = parts.Select(p =>
                new ClassifiedText(
                    p.Tag.ToClassificationTypeName(),
                    p.ToVisibleDisplayString(includeLeftToRightMarker: true)));
            return classifiedTexts.ToInlines(typeMap, classificationFormatMap);
        }

         public static IList<Inline> ToInlines(
            this IEnumerable<ClassifiedText> parts, 
            ClassificationTypeMap typeMap,
            string classificationFormatMap = null,
            Action<Run, ClassifiedText, int> runCallback = null)
        {
            classificationFormatMap = classificationFormatMap ?? "tooltip";

            var formatMap = typeMap.ClassificationFormatMapService.GetClassificationFormatMap(classificationFormatMap);
            var inlines = new List<Inline>();

            var position = 0;
            foreach (var part in parts)
            {
                var run = part.ToRun(formatMap, typeMap);
                runCallback?.Invoke(run, part, position);
                inlines.Add(run);

                position += part.Text.Length;
            }

            return inlines;
        }

        public static TextBlock ToTextBlock(
            this IEnumerable<TaggedText> parts,
            ClassificationTypeMap typeMap,
            string classificationFormatMap = null)
        {
            classificationFormatMap = classificationFormatMap ?? "tooltip";

            var inlines = parts.ToInlines(typeMap, classificationFormatMap);
            return inlines.ToTextBlock(typeMap, classificationFormatMap);
        }

        public static TextBlock ToTextBlock(
            this IEnumerable<Inline> inlines,
            ClassificationTypeMap typeMap,
            string classificationFormatMap = null,
            bool wrap = true)
        {
            classificationFormatMap = classificationFormatMap ?? "tooltip";
            var formatMap = typeMap.ClassificationFormatMapService.GetClassificationFormatMap(classificationFormatMap);

            var textBlock = new TextBlock
            {
                TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap,
                TextTrimming = wrap ? TextTrimming.None : TextTrimming.CharacterEllipsis
            };
            textBlock.SetDefaultTextProperties(formatMap);
            textBlock.Inlines.AddRange(inlines);

            return textBlock;
        }

        public static IList<ClassificationSpan> ToClassificationSpans(
            this IEnumerable<TaggedText> parts,
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
                    typeMap.GetClassificationType(part.Tag.ToClassificationTypeName())));

                index += text.Length;
            }

            return result;
        }
    }
}