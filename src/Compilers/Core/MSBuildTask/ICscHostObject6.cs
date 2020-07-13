// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]
    [Guid("2899D4C5-8C11-40AE-B501-DB1EE401E3CA")]
    public interface ICscHostObject6 : ICscHostObject5
    {
        bool SetWarnVersion(decimal warnVersion);
    }
}
