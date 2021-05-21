// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Classification
{
    internal static class ClassificationUtilities
    {
        public static TagSpan<IClassificationTag> Convert(ClassificationTypeMap typeMap, ITextSnapshot snapshot, ClassifiedSpan classifiedSpan)
        {
            return new TagSpan<IClassificationTag>(
                classifiedSpan.TextSpan.ToSnapshotSpan(snapshot),
                new ClassificationTag(typeMap.GetClassificationType(classifiedSpan.ClassificationType)));
        }

        public static List<ITagSpan<IClassificationTag>> Convert(ClassificationTypeMap typeMap, ITextSnapshot snapshot, ArrayBuilder<ClassifiedSpan> classifiedSpans)
        {
            var result = new List<ITagSpan<IClassificationTag>>(capacity: classifiedSpans.Count);
            foreach (var span in classifiedSpans)
                result.Add(Convert(typeMap, snapshot, span));

            return result;
        }
    }
}
