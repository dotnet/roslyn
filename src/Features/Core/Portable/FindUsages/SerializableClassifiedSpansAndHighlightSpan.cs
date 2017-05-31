// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindUsages
{
    internal class SerializableClassifiedSpansAndHighlightSpan
    {
        public SerializableClassifiedSpan[] ClassifiedSpans;
        public TextSpan HighlightSpan;

        internal static SerializableClassifiedSpansAndHighlightSpan Dehydrate(ClassifiedSpansAndHighlightSpan classifiedSpans)
        {
            return new SerializableClassifiedSpansAndHighlightSpan
            {
                ClassifiedSpans = SerializableClassifiedSpan.Dehydrate(classifiedSpans.ClassifiedSpans),
                HighlightSpan = classifiedSpans.HighlightSpan
            };
        }

        public ClassifiedSpansAndHighlightSpan Rehydrate()
        {
            return new ClassifiedSpansAndHighlightSpan(
                SerializableClassifiedSpan.Rehydrate(ClassifiedSpans),
                HighlightSpan);
        }
    }

    internal class SerializableClassifiedSpan
    {
        /// <summary>
        /// So we don't have to serialize over a string for each classified span we 
        /// marshal over, we keep a mapping from ClassificationTypeName to an int.
        /// This way we can just send over the int and retrieve the name on the other
        /// side.
        /// 
        /// All classification names should be from <see cref="ClassificationTypeNames"/>. 
        /// However, in case we somehow get a name not from that type, we support marshaling
        /// over the name as well.
        /// </summary>
        private static BidirectionalMap<string, int> s_classificationTypeToIndex = 
            new BidirectionalMap<string, int>(GetTypesAndIndices());

        public string ClassificationType;
        public int ClassificationTypeIndex;
        public TextSpan TextSpan;

        private static IEnumerable<KeyValuePair<string, int>> GetTypesAndIndices()
        {
            var q = from fieldInfo in typeof(ClassificationTypeNames).GetTypeInfo().DeclaredFields
                    where fieldInfo.IsStatic && fieldInfo.IsPublic && fieldInfo.FieldType == typeof(string)
                    orderby fieldInfo.Name
                    select (string)fieldInfo.GetValue(null);

            return q.Select((name, index) => new KeyValuePair<string, int>(name, index));
        }

        internal static SerializableClassifiedSpan[] Dehydrate(ImmutableArray<ClassifiedSpan> classifiedSpans)
        {
            var result = new SerializableClassifiedSpan[classifiedSpans.Length];
            int index = 0;
            foreach (var classifiedSpan in classifiedSpans)
            {
                result[index] = Dehydrate(classifiedSpan);
                index++;
            }

            return result;
        }

        private static SerializableClassifiedSpan Dehydrate(ClassifiedSpan classifiedSpan)
        {
            var classificationTypeIndex = s_classificationTypeToIndex.TryGetValue(classifiedSpan.ClassificationType, out var index)
                ? index
                : -1;

            var classificationType = classificationTypeIndex == -1
                ? classifiedSpan.ClassificationType
                : null;

            return new SerializableClassifiedSpan
            {
                ClassificationType = classificationType,
                ClassificationTypeIndex = classificationTypeIndex,
                TextSpan = classifiedSpan.TextSpan
            };
        }

        public static ImmutableArray<ClassifiedSpan> Rehydrate(SerializableClassifiedSpan[] classifiedSpans)
        {
            var result = ArrayBuilder<ClassifiedSpan>.GetInstance(classifiedSpans.Length);
            foreach (var classifiedSpan in classifiedSpans)
            {
                result.Add(classifiedSpan.Rehydrate());
            }

            return result.ToImmutableAndFree();
        }

        public ClassifiedSpan Rehydrate()
            => ClassificationTypeIndex != -1
                ? new ClassifiedSpan(s_classificationTypeToIndex.GetKeyOrDefault(ClassificationTypeIndex), TextSpan)
                : new ClassifiedSpan(ClassificationType, TextSpan);
    }
}