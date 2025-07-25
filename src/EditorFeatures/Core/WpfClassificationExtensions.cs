// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions;

internal static partial class WpfClassificationExtensions
{
    extension(ClassifiedText part)
    {
        public Run ToRun(IClassificationFormatMap formatMap, ClassificationTypeMap typeMap)
        {
            var run = new Run(part.Text);

            var classificationType = typeMap.GetClassificationType(part.ClassificationType);

            var format = formatMap.GetTextProperties(classificationType);
            run.SetTextProperties(format);

            return run;
        }
    }

    extension(IEnumerable<ClassifiedText> parts)
    {
        public IList<Inline> ToInlines(
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
    }

    extension(IEnumerable<TaggedText> parts)
    {
        public IList<Inline> ToInlines(
        IClassificationFormatMap formatMap,
        ClassificationTypeMap typeMap)
        {
            var classifiedTexts = parts.Select(p =>
                new ClassifiedText(
                    p.Tag.ToClassificationTypeName(),
                    p.ToVisibleDisplayString(includeLeftToRightMarker: true)));
            return classifiedTexts.ToInlines(formatMap, typeMap);
        }

        public TextBlock ToTextBlock(
            IClassificationFormatMap formatMap,
            ClassificationTypeMap typeMap)
        {
            var inlines = parts.ToInlines(formatMap, typeMap);
            return inlines.ToTextBlock(formatMap);
        }
    }

    extension(IEnumerable<Inline> inlines)
    {
        [Obsolete("Use 'public static TextBlock ToTextBlock(this IEnumerable <Inline> inlines, IClassificationFormatMap formatMap, bool wrap = true)' instead")]
        public TextBlock ToTextBlock(
        IClassificationFormatMap formatMap,
        ClassificationTypeMap typeMap,
        string classificationFormatMap = null,
        bool wrap = true)
        => inlines.ToTextBlock(formatMap, wrap);

        public TextBlock ToTextBlock(
            IClassificationFormatMap formatMap,
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
    }

    extension(TaggedText part)
    {
        public TextBlock ToTextBlock(IClassificationFormatMap formatMap, ClassificationTypeMap typeMap)
        => SpecializedCollections.SingletonEnumerable(part).ToTextBlock(formatMap, typeMap);
    }
}
