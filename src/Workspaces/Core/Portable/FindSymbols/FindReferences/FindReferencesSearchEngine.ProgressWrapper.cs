// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class FindReferencesSearchEngine
    {
        private class ProgressWrapper
        {
            private readonly IFindReferencesProgress _progress;
            private readonly int _maximum;
            private int _current;

            public ProgressWrapper(IFindReferencesProgress progress, int maximum)
            {
                _progress = progress;
                _maximum = maximum;
            }

            public void Increment()
            {
                var result = Interlocked.Increment(ref _current);
                _progress.ReportProgress(_current, _maximum);
            }
        }
    }
}
