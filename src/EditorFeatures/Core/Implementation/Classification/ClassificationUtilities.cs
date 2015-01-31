// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Classification
{
    internal static class ClassificationUtilities
    {
        private static readonly ConcurrentQueue<List<ClassifiedSpan>> s_spanCache = new ConcurrentQueue<List<ClassifiedSpan>>();

        public static List<ClassifiedSpan> GetOrCreateClassifiedSpanList()
        {
            List<ClassifiedSpan> result;
            return s_spanCache.TryDequeue(out result)
                ? result
                : new List<ClassifiedSpan>();
        }

        public static void ReturnClassifiedSpanList(List<ClassifiedSpan> list)
        {
            if (list == null)
            {
                return;
            }

            list.Clear();
            s_spanCache.Enqueue(list);
        }

        public static List<ITagSpan<IClassificationTag>> Convert(ClassificationTypeMap typeMap, ITextSnapshot snapshot, List<ClassifiedSpan> list)
        {
            var result = new List<ITagSpan<IClassificationTag>>();

            foreach (var classifiedSpan in list)
            {
                result.Add(new TagSpan<IClassificationTag>(
                    classifiedSpan.TextSpan.ToSnapshotSpan(snapshot),
                    new ClassificationTag(typeMap.GetClassificationType(classifiedSpan.ClassificationType))));
            }

            return result;
        }

        public static List<ITagSpan<IClassificationTag>> ConvertAndReturnList(ClassificationTypeMap typeMap, ITextSnapshot snapshot, List<ClassifiedSpan> classifiedSpans)
        {
            var result = Convert(typeMap, snapshot, classifiedSpans);
            ReturnClassifiedSpanList(classifiedSpans);
            return result;
        }
    }
}
