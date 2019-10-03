// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare.Client.Classification
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
