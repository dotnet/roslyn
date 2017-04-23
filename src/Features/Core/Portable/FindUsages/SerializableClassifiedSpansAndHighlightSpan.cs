// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Text;

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
        public string ClassificationType;
        public TextSpan TextSpan;

        internal static SerializableClassifiedSpan[] Dehydrate(ImmutableArray<ClassifiedSpan> classifiedSpans)
        {
            var result = new SerializableClassifiedSpan[classifiedSpans.Length];
            int index = 0;
            foreach (var classifiedSpan in classifiedSpans)
            {
                result[index] = SerializableClassifiedSpan.Dehydrate(classifiedSpan);
                index++;
            }

            return result;
        }

        private static SerializableClassifiedSpan Dehydrate(ClassifiedSpan classifiedSpan)
        {
            return new SerializableClassifiedSpan
            {
                ClassificationType = classifiedSpan.ClassificationType,
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
            => new ClassifiedSpan(ClassificationType, TextSpan);
    }
}