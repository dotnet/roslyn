// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Razor.LiveShare;

// Retained for compatibility with the published RPC interface.
public interface IProjectHierarchyProxy
{
    Task<Uri?> GetProjectPathAsync(Uri documentFilePath, CancellationToken cancellationToken);
}
