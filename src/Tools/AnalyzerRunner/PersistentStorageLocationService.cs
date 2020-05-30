// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace AnalyzerRunner
{
    [ExportWorkspaceService(typeof(IPersistentStorageLocationService), ServiceLayer.Host)]
    [Shared]
    internal class PersistentStorageLocationService : IPersistentStorageLocationService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PersistentStorageLocationService()
        {
        }

        public bool IsSupported(Workspace workspace) => true;

        public string TryGetStorageLocation(Solution _)
        {
            var location = Path.Combine(Path.GetTempPath(), "RoslynTests", "AnalyzerRunner", "temp-db");
            Directory.CreateDirectory(location);
            return location;
        }
    }
}
