// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Roslyn.Utilities;

internal static class ProcessStartInfoExtensions
{
    /// <summary>
    /// Removes diagnostic port settings that target the current .NET runtime and must not be inherited by child processes.
    /// </summary>
    public static void RemoveInheritedDotNetDiagnosticPorts(this ProcessStartInfo processStartInfo)
    {
        processStartInfo.Environment.Remove("DOTNET_DiagnosticPorts");
        processStartInfo.Environment.Remove("DOTNET_DefaultDiagnosticPortSuspend");
    }
}
