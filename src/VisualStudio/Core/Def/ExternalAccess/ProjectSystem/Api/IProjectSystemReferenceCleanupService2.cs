// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.ProjectSystem.Api
{
    // Interface to be implemented and MEF exported by Project System
    internal interface IProjectSystemReferenceCleanupService2 : IProjectSystemReferenceCleanupService
    {
        /// <summary>
        /// Gets an operation that can update the project’s references by removing or marking references as
        /// TreatAsUsed in the project file.
        /// </summary>
        Task<IProjectSystemUpdateReferenceOperation> GetUpdateReferenceOperationAsync(
            string projectPath,
            ProjectSystemReferenceUpdate referenceUpdate,
            CancellationToken canellationToken);
    }
}
