// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CodeAnalysis.CSharp;

internal static class EnvironmentExtensions
{
    extension(Environment)
    {
        public static int ProcessId
        {
            get
            {
#if NET
                return Environment.ProcessId;
#else
                return System.Diagnostics.Process.GetCurrentProcess().Id;
#endif
            }
        }
    }
}
