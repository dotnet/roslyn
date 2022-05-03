// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Classification
{
    internal partial class SyntacticClassificationTaggerProvider
    {
        internal partial class TagComputer
        {
            /// <summary>
            /// it is a helper class that encapsulates logic on holding onto last classification result
            /// </summary>
            private class LastLineCache : ForegroundThreadAffinitizedObject
            {
                // this helper class is primarily to improve active typing perf. don't bother to cache
                // something very big. 
                private const int MaxClassificationNumber = 32;

                // mutating state
                private SnapshotSpan _span;
                private readonly ArrayBuilder<ClassifiedSpan> _classifications = new();

                public LastLineCache(IThreadingContext threadingContext) : base(threadingContext)
                {
                }

                private void Clear()
                {
                    this.AssertIsForeground();

                    _span = default;
                    _classifications.Clear();
                }

                public bool TryUseCache(SnapshotSpan span, ArrayBuilder<ClassifiedSpan> classifications)
                {
                    this.AssertIsForeground();

                    // currently, it is using SnapshotSpan even though holding onto it could be
                    // expensive. reason being it should be very soon sync-ed to latest snapshot.
                    if (_span.Equals(span))
                    {
                        classifications.AddRange(_classifications);
                        return true;
                    }

                    this.Clear();
                    return false;
                }

                public void Update(SnapshotSpan span, ArrayBuilder<ClassifiedSpan> classifications)
                {
                    this.AssertIsForeground();
                    this.Clear();

                    if (classifications.Count < MaxClassificationNumber)
                    {
                        _span = span;
                        _classifications.AddRange(classifications);
                    }
                }
            }
        }
    }
}
