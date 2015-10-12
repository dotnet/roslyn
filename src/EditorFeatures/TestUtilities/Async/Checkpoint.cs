// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public Task Task { get { return _tcs.Task; } }

        public void Release()
        {
            _tcs.TrySetResult(null);
        }
    }
}
