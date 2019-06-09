// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Classification
{
    internal partial class SyntacticClassificationTaggerProvider
    {
        internal partial class TagComputer
        {
            /// <summary>
            /// it is a helper class that encapsulates logic on holding onto last classification result
            /// </summary>
            private class LastLineCache
            {
                // this helper class is primarily to improve active typing perf. don't bother to cache
                // something very big. 
                private const int MaxClassificationNumber = 32;
                private readonly object _gate = new object();

                // mutating state
                private SnapshotSpan _span;
                private List<ClassifiedSpan> _classifications;

                public LastLineCache()
                {
                    this.Clear();
                }

                private void Clear()
                {
                    lock (_gate)
                    {
                        _span = default;
                        ClassificationUtilities.ReturnClassifiedSpanList(_classifications);
                        _classifications = null;
                    }
                }

                public bool TryUseCache(SnapshotSpan span, out List<ClassifiedSpan> classifications)
                {
                    lock (_gate)
                    {
                        // currently, it is using SnapshotSpan even though holding onto it could be
                        // expensive. reason being it should be very soon sync-ed to latest snapshot.
                        if (_classifications != null && _span.Equals(span))
                        {
                            classifications = _classifications;
                            return true;
                        }

                        this.Clear();
                        classifications = null;
                        return false;
                    }
                }

                public void Update(SnapshotSpan span, List<ClassifiedSpan> classifications)
                {
                    lock (_gate)
                    {
                        this.Clear();

                        if (classifications.Count < MaxClassificationNumber)
                        {
                            _span = span;
                            _classifications = classifications;
                        }
                    }
                }
            }
        }
    }
}
