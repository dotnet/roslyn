// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class DocumentState
    {
        internal class IncrementalTreeSource : ValueSource<TreeAndVersion>
        {
            private readonly ValueSource<TreeAndVersion> _current;
            private readonly ValueSource<TreeAndVersion> _previous;
 
            public IncrementalTreeSource(ValueSource<TreeAndVersion> current, ValueSource<TreeAndVersion> previous)
            {
                _current = current;
                _previous = previous;
            }

            /// <summary>
            /// Computes the number of incremental parses that will occur in order to realize the current source's tree.
            /// </summary>
            public int GetIncrementalParseDepth()
            {
                int depth = 0;
                for (var source = _previous as IncrementalTreeSource;
                    source != null;
                    source = source._previous as IncrementalTreeSource)
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