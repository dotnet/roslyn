// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Debugger.Contracts.EditAndContinue;

namespace Microsoft.CodeAnalysis.ExternalAccess.DotNetWatch
{
    internal enum DotNetWatchManagedModuleUpdateStatus
    {
        None = ManagedModuleUpdateStatus.None,
        Ready = ManagedModuleUpdateStatus.Ready,
        Blocked = ManagedModuleUpdateStatus.Blocked,
    }
}
