// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Storage;

namespace AnalyzerRunner
{
    [ExportWorkspaceService(typeof(IPersistentStorageConfiguration), ServiceLayer.Host)]
    [Shared]
    internal sealed class PersistentStorageConfiguration : IPersistentStorageConfiguration
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PersistentStorageConfiguration()
        {
        }

        public bool ThrowOnFailure => true;

        public string? TryGetStorageLocation(SolutionKey _)
        {
            var location = Path.Combine(Path.GetTempPath(), "RoslynTests", "AnalyzerRunner", "temp-db");
            Directory.CreateDirectory(location);
            return location;
        }
    }
}
