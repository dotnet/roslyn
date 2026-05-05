// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Razor.Utilities;

internal static class PlatformInformation
{
    public static bool IsWindows { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static bool IsLinux { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    public static bool IsMacOS { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

#if NET
    public static bool IsFreeBSD { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);
#else
    public static bool IsFreeBSD { get; } = false;
#endif
}
