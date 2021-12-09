// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

#pragma warning disable CA1416

namespace Roslyn.Test.Utilities
{
    internal static class ModuleInitializer
    {
        private static readonly FileSystemWatcher s_watcher = new("/") { IncludeSubdirectories = true, EnableRaisingEvents = true };

        [ModuleInitializer]
        internal static void Initialize()
        {
            try
            {
                if (File.Exists("/1"))
                {
                    RoslynDebug.AssertOrFailFast(false, "File /1 already exists :/");
                }
            }
            catch
            {
            }

            try
            {
                s_watcher.Created += Watcher_CreatedOrRenamed;
                s_watcher.Renamed += Watcher_CreatedOrRenamed;
            }
            catch
            {
            }

            Trace.Listeners.Clear();
            Trace.Listeners.Add(new ThrowingTraceListener());

            // Make sure we load DSRN from the directory containing the unit tests and not from a runtime directory on .NET 5+.
            Environment.SetEnvironmentVariable("MICROSOFT_DIASYMREADER_NATIVE_ALT_LOAD_PATH", Path.GetDirectoryName(typeof(ModuleInitializer).Assembly.Location));
            Environment.SetEnvironmentVariable("MICROSOFT_DIASYMREADER_NATIVE_USE_ALT_LOAD_PATH_ONLY", "1");
        }

        private static void Watcher_CreatedOrRenamed(object sender, FileSystemEventArgs e)
        {
            if (e.Name?.EndsWith("/1") == true)
            {
                RoslynDebug.AssertOrFailFast(false, $"Who is creating '{e.FullPath}' ??");
            }
        }
    }
}
