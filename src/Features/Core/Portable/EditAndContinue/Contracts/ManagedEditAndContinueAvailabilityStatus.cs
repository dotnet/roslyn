// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue
{
    internal enum ManagedEditAndContinueAvailabilityStatus
    {
        Available,
        Interop,
        SqlClr,
        Minidump,
        Attach,
        ModuleNotLoaded,
        ModuleReloaded,
        InRunMode,
        NotBuilt,
        EngineMetricFalse,
        NotSupportedForClr64Version,
        NotAllowedForModule,
        Optimized,
        DomainNeutralAssembly,
        ReflectionAssembly,
        IntelliTrace,
        NotAllowedForRuntime,
        InternalError
    }
}
