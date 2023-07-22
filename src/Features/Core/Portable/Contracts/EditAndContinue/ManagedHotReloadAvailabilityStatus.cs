// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Contracts.EditAndContinue
{
    /// <summary>
    /// Availability status for applying changes under a session.
    /// </summary>
    /// <remarks>
    /// Do not change the value for any of the preexisting status, as this is the value 
    /// used when reporting telemetry.
    /// </remarks>
    internal enum ManagedHotReloadAvailabilityStatus
    {
        /// <summary>
        /// Applying changes is available to the current session.
        /// </summary>
        Available = 0,

        /// <summary>
        /// Edit and Continue not supported due to interop debugging.
        /// </summary>
        Interop = 1,

        /// <summary>
        /// Unable to edit code running in SQL server.
        /// </summary>
        SqlClr = 2,

        /// <summary>
        /// Edit and Continue not supported in minidump debugging.
        /// </summary>
        Minidump = 3,

        /// <summary>
        /// Edit and Continue not supported since debugger was attached to a process that
        /// does not support EnC on attach.
        /// </summary>
        Attach = 4,

        /// <summary>
        /// Edit and Continue not supported if the assembly has not been loaded.
        /// </summary>
        ModuleNotLoaded = 5,

        /// <summary>
        /// Edit and Continue not supported if the assembly that has been modified during
        /// debugging is reloaded.
        /// </summary>
        ModuleReloaded = 6,

        /// <summary>
        /// Edit and Continue not supported if the source code on disk does not match the
        /// code running in the process.
        /// </summary>
        NotBuilt = 8,

        /// <summary>
        /// Edit and Continue not supported for the current engine.
        /// </summary>
        UnsupportedEngine = 9,

        /// <summary>
        /// Edit and Continue in a 64-bit process requires .NET Framework version 4.5.1 or
        /// higher.
        /// </summary>
        NotSupportedForClr64Version = 10,

        /// <summary>
        /// Edit and Continue not supported on the current module. This is a fallback
        /// scenario in case we fail to determine the exact reason the module does not
        /// support EnC.
        /// </summary>
        NotAllowedForModule = 11,

        /// <summary>
        /// Edit and Continue not supported if code was optimized.
        /// </summary>
        Optimized = 12,

        /// <summary>
        /// Edit and Continue not supported if assembly was loaded as domain-neutral.
        /// </summary>
        DomainNeutralAssembly = 13,

        /// <summary>
        /// Edit and Continue not supported if assembly was loaded through reflection.
        /// </summary>
        ReflectionAssembly = 14,

        /// <summary>
        /// Edit and Continue not supported if IntelliTrace events and call information is
        /// enabled.
        /// </summary>
        IntelliTrace = 15,

        /// <summary>
        /// Edit and Continue not supported on the .NET Runtime the program is running.
        /// </summary>
        NotAllowedForRuntime = 16,

        /// <summary>
        /// Edit and Continue not supported due to an internal error in the debugger.
        /// </summary>
        InternalError = 17,

        /// <summary>
        /// Edit and Continue is unavailable, e.g. no suitable engine providers were found.
        /// </summary>
        Unavailable = 18,

        /// <summary>
        /// Applying changes has been disabled by the client.
        /// If debugging, this means Edit and Continue has been disabled.
        /// If not debugging, this means hot reload has been disabled.
        /// </summary>
        Disabled = 19
    };
}
