// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
