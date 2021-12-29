// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Api
{
    internal interface IProjectSystemUpdateReferenceOperation
    {
        /// <summary>
        /// Applies a reference update operation to the project file.
        /// </summary>
        /// <returns>A boolean indicating success.</returns>
        /// <remarks>Throws <see cref="InvalidOperationException"/> if operation has already been applied.</remarks>
        Task<bool> ApplyAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Reverts a reference update operation to the project file.
        /// </summary>
        /// <returns>A boolean indicating success.</returns>
        /// <remarks>Throws <see cref="InvalidOperationException"/> if operation has not been applied.</remarks>
        Task<bool> RevertAsync(CancellationToken cancellationToken);
    }
}
