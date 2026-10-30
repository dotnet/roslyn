// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.Razor.LiveShare;

// Retains the published RPC identity and declared members.
public interface IRemoteHierarchyService
{
    Task<bool> HasCapabilityAsync(Uri pathOfFileInProject, string capability, CancellationToken cancellationToken);
}
