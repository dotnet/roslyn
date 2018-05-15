// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static partial class ClassificationExtensions
    {
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
                    new SnapshotSpan(textSnapshot, new Span(index, text.Length)),
                    typeMap.GetClassificationType(part.Tag.ToClassificationTypeName())));

                index += text.Length;
            }

            return result;
        }
    }
}
