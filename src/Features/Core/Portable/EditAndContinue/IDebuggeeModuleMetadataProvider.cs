// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    /// <summary>
    /// Provides metadata of modules loaded into processes being debugged.
    /// </summary>
    internal interface IDebuggeeModuleMetadataProvider
    {
        /// <summary>
        /// Finds a module of given MVID in one of the processes being debugged and returns its baseline metadata and symbols.
        /// Shall only be called while in debug mode.
        /// Shall only be called on MTA thread.
        /// </summary>
        /// <returns>Null, if the module with the specified MVID is not loaded.</returns>
        DebuggeeModuleInfo? TryGetBaselineModuleInfo(Guid mvid);

        /// <summary>
        /// Checks whether EnC is allowed for all loaded instances of module with specified <paramref name="mvid"/>.
        /// </summary>
        /// <returns>
        /// Returns <see langword="null"/> if no instance of the module is loaded.
        /// Returns <code>(0, null)</code> if all loaded instances allow EnC.
        /// Returns error code and a corresponding localized error message otherwise.
        /// </returns>
        Task<(int errorCode, string? errorMessage)?> GetEncAvailabilityAsync(Guid mvid, CancellationToken cancellationToken);

        /// <summary>
        /// Notifies the debugger that a document changed that may affect the given module when the change is applied.
        /// </summary>
        Task PrepareModuleForUpdateAsync(Guid mvid, CancellationToken cancellationToken);
    }
}
