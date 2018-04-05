// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Remote
{
    internal static class WellKnownRemoteHostServices
    {
        public static void Set64bit(bool x64)
        {
            RemoteHostService = "roslynRemoteHost" + (x64 ? "64" : "");
        }

        public static string RemoteHostService { get; private set; } = "roslynRemoteHost";
    }
}
