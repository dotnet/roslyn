// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Razor.LiveShare;

// This type must be a public interface in order to to be implemented as an RPC proxy by live share.
public interface IProjectHierarchyProxy
{
    Task<Uri?> GetProjectPathAsync(Uri documentFilePath, CancellationToken cancellationToken);
}
