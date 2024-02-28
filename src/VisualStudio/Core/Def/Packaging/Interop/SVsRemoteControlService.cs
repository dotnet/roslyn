// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Runtime.InteropServices;

namespace Microsoft.Internal.VisualStudio.Shell.Interop;

[Guid("BF443850-E346-44A8-B03C-11B15ACDEEC1")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface SVsRemoteControlService
{
}

internal enum __VsRemoteControlBehaviorOnStale
{
    /// <summary>
    /// Returns the last locally cached file for this URL or null if no locally cached file found.
    /// </summary>
    ReturnsStale = 0,

    /// <summary>
    /// If the locally cached file exists and it was checked against the server less than pollingIntervalMinutes (specified in CreateClient) ago, returns that. Otherwise null.
    /// </summary>
    ReturnsNull = 1
}
