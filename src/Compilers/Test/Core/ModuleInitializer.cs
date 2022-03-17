// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace Roslyn.Test.Utilities
{
    internal static class ModuleInitializer
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new ThrowingTraceListener());
        }
    }
}
