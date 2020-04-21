// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Storage;

namespace Microsoft.CodeAnalysis.SQLite
{
    /// <summary>
    /// Base type of the v1 <see cref="v1.SQLitePersistentStorageService"/> and v2 <see cref="v2.SQLitePersistentStorageService"/>.
    /// Used as a common location for the common static helpers to load the sqlite pcl library.
    /// </summary>
    internal abstract class AbstractSQLitePersistentStorageService : AbstractPersistentStorageService
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr LoadLibrary(string dllToLoad);

        protected static bool TryInitializeLibraries() => s_initialized.Value;

        private static readonly Lazy<bool> s_initialized = new Lazy<bool>(() => TryInitializeLibrariesLazy());

        private static bool TryInitializeLibrariesLazy()
        {
            // Attempt to load the correct version of e_sqlite.dll.  That way when we call
            // into SQLitePCL.Batteries_V2.Init it will be able to find it.
            //
            // Only do this on Windows when we can safely do the LoadLibrary call to this
            // direct dll.  On other platforms, it is the responsibility of the host to ensure
            // that the necessary sqlite library has already been loaded such that SQLitePCL.Batteries_V2
            // will be able to call into it.
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                var myFolder = Path.GetDirectoryName(
                    typeof(AbstractSQLitePersistentStorageService).Assembly.Location);
                if (myFolder == null)
                    return false;

                var is64 = IntPtr.Size == 8;
                var subfolder = is64 ? "x64" : "x86";

                LoadLibrary(Path.Combine(myFolder, subfolder, "e_sqlite3.dll"));
            }

            try
            {
                // Necessary to initialize SQLitePCL.
                SQLitePCL.Batteries_V2.Init();
            }
            catch (Exception e) when (e is DllNotFoundException || e is EntryPointNotFoundException)
            {
                StorageDatabaseLogger.LogException(e);
                return false;
            }

            return true;
        }

        protected AbstractSQLitePersistentStorageService(IPersistentStorageLocationService locationService)
            : base(locationService)
        {
        }
    }
}
