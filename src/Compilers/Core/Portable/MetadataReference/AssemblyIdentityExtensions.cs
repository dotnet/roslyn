// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
