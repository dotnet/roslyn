// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class FindReferencesSearchEngine
    {
        private class ProgressWrapper
        {
            private readonly IStreamingFindReferencesProgress _progress;
            private readonly int _maximum;
            private int _current;

            public ProgressWrapper(IStreamingFindReferencesProgress progress, int maximum)
            {
                _progress = progress;
                _maximum = maximum;
            }

            public Task IncrementAsync()
            {
                var result = Interlocked.Increment(ref _current);
                return _progress.ReportProgressAsync(_current, _maximum);
            }
        }
    }
}