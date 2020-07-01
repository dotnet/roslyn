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
