// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions;

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
