// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        public bool IsSupported(Workspace workspace) => true;

        public string TryGetStorageLocation(Solution _)
        {
            var location = Path.Combine(Path.GetTempPath(), "RoslynTests", "AnalyzerRunner", "temp-db");
            Directory.CreateDirectory(location);
            return location;
        }
    }
}
