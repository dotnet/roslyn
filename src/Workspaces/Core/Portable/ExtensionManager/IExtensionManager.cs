// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Extensions
{
    internal interface IExtensionManager : IWorkspaceService
    {
        bool IsDisabled(object provider);

        bool CanHandleException(object provider, Exception exception);

        void HandleException(object provider, Exception exception);
    }
}
