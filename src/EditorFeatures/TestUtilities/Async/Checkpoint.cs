// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Roslyn.Test.Utilities
{
    public class Checkpoint
    {
        private readonly TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();

        public Task Task => _tcs.Task;

        public void Release() => _tcs.SetResult(null);
    }
}
