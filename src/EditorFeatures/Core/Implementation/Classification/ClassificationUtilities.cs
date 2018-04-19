﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Classification
{
    internal static class ClassificationUtilities
    {
        private static readonly ConcurrentQueue<List<ClassifiedSpan>> s_spanCache = new ConcurrentQueue<List<ClassifiedSpan>>();

        public static List<ClassifiedSpan> GetOrCreateClassifiedSpanList()
        {
            return s_spanCache.TryDequeue(out var result)
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

        public static void Convert(ClassificationTypeMap typeMap, ITextSnapshot snapshot, List<ClassifiedSpan> list, Action<ITagSpan<IClassificationTag>> addTag)
        {
            foreach (var classifiedSpan in list)
            {
                IClassificationType classificationType;
                switch (classifiedSpan.ClassificationType) // filter out unsupported classification types
                {
                    case ClassificationTypeNames.FieldName:
                    case ClassificationTypeNames.EnumMemberName:
                    case ClassificationTypeNames.ConstantName:
                    case ClassificationTypeNames.LocalName:
                    case ClassificationTypeNames.ParameterName:
                    case ClassificationTypeNames.MethodName:
                    case ClassificationTypeNames.ExtensionMethodName:
                    case ClassificationTypeNames.PropertyName:
                    case ClassificationTypeNames.EventName:
                        classificationType = typeMap.GetClassificationType(ClassificationTypeNames.Identifier);
                        break;
                    default:
                        classificationType = typeMap.GetClassificationType(classifiedSpan.ClassificationType);
                        break;
                }

                addTag(new TagSpan<IClassificationTag>(
                    classifiedSpan.TextSpan.ToSnapshotSpan(snapshot),
                    new ClassificationTag(classificationType)));
            }
        }

        public static List<ITagSpan<IClassificationTag>> ConvertAndReturnList(ClassificationTypeMap typeMap, ITextSnapshot snapshot, List<ClassifiedSpan> classifiedSpans)
        {
            var result = new List<ITagSpan<IClassificationTag>>();
            Convert(typeMap, snapshot, classifiedSpans, result.Add);
            ReturnClassifiedSpanList(classifiedSpans);
            return result;
        }
    }
}
