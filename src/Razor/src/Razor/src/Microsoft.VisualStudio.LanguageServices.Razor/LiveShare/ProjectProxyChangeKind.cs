// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.VisualStudio.Razor.LiveShare;

// This type must be public because it is exposed by a public interface that is implemented as
// an RPC proxy by live share.
public enum ProjectProxyChangeKind
{
    ProjectAdded,
    ProjectRemoved,
    ProjectChanged,
}
