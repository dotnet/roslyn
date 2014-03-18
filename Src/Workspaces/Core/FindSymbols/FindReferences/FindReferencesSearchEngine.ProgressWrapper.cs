// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal partial class FindReferencesSearchEngine
    {
        private class ProgressWrapper
        {
            private readonly IFindReferencesProgress progress;
            private readonly int maximum;
            private int current;

            public ProgressWrapper(IFindReferencesProgress progress, int maximum)
            {
                this.progress = progress;
                this.maximum = maximum;
            }

            public void Increment()
            {
                var result = Interlocked.Increment(ref current);
                progress.ReportProgress(current, maximum);
            }
        }
    }
}