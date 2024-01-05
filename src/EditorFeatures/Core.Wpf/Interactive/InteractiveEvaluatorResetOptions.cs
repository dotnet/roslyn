// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias InteractiveHost;
using InteractiveHost::Microsoft.CodeAnalysis.Interactive;

namespace Microsoft.CodeAnalysis.Interactive
{
    internal sealed class InteractiveEvaluatorResetOptions
    {
        public readonly InteractiveHostPlatform? Platform;

        public InteractiveEvaluatorResetOptions(InteractiveHostPlatform? platform)
            => Platform = platform;
    }
}
