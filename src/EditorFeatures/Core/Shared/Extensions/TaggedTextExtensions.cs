// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Roslyn.Utilities;
using System;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static partial class TaggedTextExtensions
    {
        public static Run ToRun(this TaggedText part, IClassificationFormatMap formatMap, ClassificationTypeMap typeMap)
        {
            var text = part.ToVisibleDisplayString(includeLeftToRightMarker: true);

            var run = new Run(text);

            var format = formatMap.GetTextProperties(typeMap.GetClassificationType(
                part.Tag.ToClassificationTypeName()));
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
            string classificationFormatMap = null,
            Action<Run, TaggedText, int> runCallback = null)
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
            string classificationFormatMap = null,
            Action<Run, TaggedText, int> runCallback = null)
        {
            classificationFormatMap = classificationFormatMap ?? "tooltip";

            var inlines = parts.ToInlines(typeMap, classificationFormatMap, runCallback);
            return inlines.ToTextBlock(typeMap, classificationFormatMap);
        }

        public static TextBlock ToTextBlock(
            this IEnumerable<Inline> inlines,
            ClassificationTypeMap typeMap,
            string classificationFormatMap = null)
        {
            classificationFormatMap = classificationFormatMap ?? "tooltip";
            var formatMap = typeMap.ClassificationFormatMapService.GetClassificationFormatMap(classificationFormatMap);

            var textBlock = new TextBlock { TextWrapping = TextWrapping.Wrap };
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