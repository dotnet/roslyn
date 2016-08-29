// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;
using System;

namespace Microsoft.CodeAnalysis
{
    internal partial class DocumentState
    {
        internal class IncrementalTreeSource : ValueSource<TreeAndVersion>
        {
            private readonly AsyncLazy<TreeAndVersion> _current;
            private readonly WeakReference<ValueSource<TreeAndVersion>> _weakPrevious;
 
            public IncrementalTreeSource(AsyncLazy<TreeAndVersion> current, ValueSource<TreeAndVersion> previous)
            {
                _current = current;

                // Hold onto previous source weakly, so that we don't keep realized trees alive longer than necessary.
                // note: it is expected that _current will have a strong reference on _previous until _current is realized. 
                _weakPrevious = new WeakReference<ValueSource<TreeAndVersion>>(previous);
            }

            /// <summary>
            /// Computes the number of incremental parses that will occur in order to realize the current source's tree.
            /// </summary>
            public int GetIncrementalParseDepth()
            {
                int depth = 0;

                for (var source = this;
                    source != null;
                    source = source.GetPreviousIncrementalSource())
                {
                    TreeAndVersion tmp;
                    if (source.TryGetValue(out tmp))
                    {
                        // the tree from this source has already been realized.
                        return depth;
                    }
                    else
                    {
                        depth++;
                    }
                }

                return depth;
            }

            private IncrementalTreeSource GetPreviousIncrementalSource()
            {
                ValueSource<TreeAndVersion> previous;
                _weakPrevious.TryGetTarget(out previous);
                return previous as IncrementalTreeSource;
            }

            public override bool TryGetValue(out TreeAndVersion value)
            {
                return _current.TryGetValue(out value);
            }

            public override TreeAndVersion GetValue(CancellationToken cancellationToken = default(CancellationToken))
            {
                return _current.GetValue(cancellationToken);
            }

            public override Task<TreeAndVersion> GetValueAsync(CancellationToken cancellationToken = default(CancellationToken))
            {
                return _current.GetValueAsync(cancellationToken);
            }
        }
    }
}