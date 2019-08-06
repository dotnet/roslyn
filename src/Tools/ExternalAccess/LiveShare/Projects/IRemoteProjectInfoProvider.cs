// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.LiveShare.Projects
{
    interface IRemoteProjectInfoProvider
    {
        Task<ImmutableArray<ProjectInfo>> GetRemoteProjectInfosAsync(CancellationToken cancellationToken);
    }
}
