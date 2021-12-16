// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace Microsoft.CodeAnalysis.FlowAnalysis
{
    internal sealed partial class ControlFlowGraphBuilder
    {
        internal class CaptureIdDispenser
        {
            private int _captureId = -1;

            public int GetNextId()
            {
                return Interlocked.Increment(ref _captureId);
            }

            public int GetCurrentId()
            {
                return _captureId;
            }
        }
    }
}
