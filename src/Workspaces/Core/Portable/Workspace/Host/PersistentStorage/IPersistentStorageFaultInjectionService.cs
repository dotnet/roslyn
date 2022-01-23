// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;

namespace Microsoft.CodeAnalysis.Host
{
    internal interface IPersistentStorageFaultInjector
    {
        void OnNewConnection();
        void OnFatalError(Exception ex);
    }
}
