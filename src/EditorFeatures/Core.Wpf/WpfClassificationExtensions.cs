﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    internal static partial class WpfClassificationExtensions
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

        public static IList<Inline> ToInlines(
           this IEnumerable<ClassifiedText> parts,
           IClassificationFormatMap formatMap,
           ClassificationTypeMap typeMap,
           Action<Run, ClassifiedText, int> runCallback = null)
        {
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

        public static IList<Inline> ToInlines(
            this IEnumerable<TaggedText> parts,
            IClassificationFormatMap formatMap,
            ClassificationTypeMap typeMap)
        {
            var classifiedTexts = parts.Select(p =>
                new ClassifiedText(
                    p.Tag.ToClassificationTypeName(),
                    p.ToVisibleDisplayString(includeLeftToRightMarker: true)));
            return classifiedTexts.ToInlines(formatMap, typeMap);
        }

        public static TextBlock ToTextBlock(
            this IEnumerable<TaggedText> parts,
            IClassificationFormatMap formatMap,
            ClassificationTypeMap typeMap)
        {
            var inlines = parts.ToInlines(formatMap, typeMap);
            return inlines.ToTextBlock(formatMap, typeMap);
        }

        public static TextBlock ToTextBlock(
            this IEnumerable<Inline> inlines,
            IClassificationFormatMap formatMap,
            ClassificationTypeMap typeMap,
            string classificationFormatMap = null,
            bool wrap = true)
        {

            var textBlock = new TextBlock
            {
                TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap,
                TextTrimming = wrap ? TextTrimming.None : TextTrimming.CharacterEllipsis
            };
            textBlock.SetDefaultTextProperties(formatMap);
            textBlock.Inlines.AddRange(inlines);

            return textBlock;
        }

        public static TextBlock ToTextBlock(this TaggedText part, IClassificationFormatMap formatMap, ClassificationTypeMap typeMap)
        {
            return SpecializedCollections.SingletonEnumerable(part).ToTextBlock(formatMap, typeMap);
        }
    }
}
