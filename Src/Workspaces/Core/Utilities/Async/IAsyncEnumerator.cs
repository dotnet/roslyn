// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Roslyn.Utilities
{
    internal interface IAsyncEnumerator<out T> : IDisposable
    {
        T Current { get; }
        Task<bool> MoveNextAsync(CancellationToken cancellationToken);
    }
}
