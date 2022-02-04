// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Reflection;

namespace Microsoft.CodeAnalysis
{
    internal static class AssemblyIdentityExtensions
    {
        // Windows.*[.winmd]
        internal static bool IsWindowsComponent(this AssemblyIdentity identity)
        {
            return (identity.ContentType == AssemblyContentType.WindowsRuntime) &&
                identity.Name.StartsWith("windows.", StringComparison.OrdinalIgnoreCase);
        }

        // Windows[.winmd]
        internal static bool IsWindowsRuntime(this AssemblyIdentity identity)
        {
            return (identity.ContentType == AssemblyContentType.WindowsRuntime) &&
                string.Equals(identity.Name, "windows", StringComparison.OrdinalIgnoreCase);
        }
    }
}
