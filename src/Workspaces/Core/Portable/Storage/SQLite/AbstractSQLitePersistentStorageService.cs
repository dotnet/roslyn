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
    /// Base type <see cref="v2.SQLitePersistentStorageService"/>.  Used as a common location for the common static
    /// helpers to load the sqlite pcl library.
    /// </summary>
    internal abstract class AbstractSQLitePersistentStorageService : AbstractPersistentStorageService
    {
        protected static bool TryInitializeLibraries() => s_initialized.Value;

        private static readonly Lazy<bool> s_initialized = new(() => TryInitializeLibrariesLazy());

        private static bool TryInitializeLibrariesLazy()
        {
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
