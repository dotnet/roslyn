// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.Internal.Log
{
    internal sealed class RequestLatencyTracker : IDisposable
    {
        private readonly int _tick;
        private readonly SyntacticLspLogger.RequestType _requestType;

        public RequestLatencyTracker(SyntacticLspLogger.RequestType requestType)
        {
            _tick = Environment.TickCount;
            _requestType = requestType;
        }

        public void Dispose()
        {
            var delta = Environment.TickCount - _tick;
            SyntacticLspLogger.LogRequestLatency(_requestType, delta);
        }
    }
}
