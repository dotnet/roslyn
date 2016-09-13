// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Classification
{
    /// <summary>
    /// A context for aggregating classifications from one or more <see cref="ClassificationProvider"/>'s.
    /// </summary>
    internal class ClassificationContext
    {
        private readonly List<ClassifiedSpan> _spans;
        private readonly HashSet<ClassifiedSpan> _spanSet;

        internal ClassificationContext(List<ClassifiedSpan> classifiedSpans, HashSet<ClassifiedSpan> spanSet)
        {
            _spans = classifiedSpans;
            _spanSet = spanSet;
        }

        public void AddClassification(ClassifiedSpan classifiedSpan)
        {
            if (_spanSet.Add(classifiedSpan))
            {
                _spans.Add(classifiedSpan);
            }
        }
    }
}
