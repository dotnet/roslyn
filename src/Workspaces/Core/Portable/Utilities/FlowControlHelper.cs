// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Roslyn.Utilities
{
    internal static class FlowControlHelper
    {
        public static AsyncFlowControlHelper TrySuppressFlow()
            => new(ExecutionContext.IsFlowSuppressed() ? default : ExecutionContext.SuppressFlow());

        public readonly struct AsyncFlowControlHelper : IDisposable
        {
            private readonly AsyncFlowControl _asyncFlowControl;

            public AsyncFlowControlHelper(AsyncFlowControl asyncFlowControl)
            {
                _asyncFlowControl = asyncFlowControl;
            }

            public void Dispose()
            {
                if (_asyncFlowControl != default)
                {
                    _asyncFlowControl.Dispose();
                }
            }
        }
    }
}
