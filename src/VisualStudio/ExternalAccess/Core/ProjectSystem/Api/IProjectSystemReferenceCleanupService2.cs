// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.LanguageServices.ExternalAccess.Unified.ProjectSystem.Api;

internal interface IProjectSystemReferenceCleanupService2 : IProjectSystemReferenceCleanupService
{
    Task<IProjectSystemUpdateReferenceOperation> GetUpdateReferenceOperationAsync(string projectPath, ProjectSystemReferenceUpdate referenceUpdate, CancellationToken cancellationToken);
}
