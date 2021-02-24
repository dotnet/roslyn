// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.Interactive
{
    internal sealed class RemoteInitializationResult
    {
        internal sealed class Data
        {
            public string? ScriptPath;
            public string[] MetadataReferencePaths = null!;
            public string[] Imports = null!;

            public RemoteInitializationResult Deserialize()
                => new RemoteInitializationResult(
                    ScriptPath,
                    ImmutableArray.Create(MetadataReferencePaths),
                    ImmutableArray.Create(Imports));
        }

        /// <summary>
        /// Full path to the initialization script that has been executed as part of initialization process.
        /// </summary>
        public readonly string? ScriptPath;

        public readonly ImmutableArray<string> MetadataReferencePaths;

        public readonly ImmutableArray<string> Imports;

        public RemoteInitializationResult(string? initializationScript, ImmutableArray<string> metadataReferencePaths, ImmutableArray<string> imports)
        {
            ScriptPath = initializationScript;
            MetadataReferencePaths = metadataReferencePaths;
            Imports = imports;
        }

        public Data Serialize()
            => new Data()
            {
                ScriptPath = ScriptPath,
                MetadataReferencePaths = MetadataReferencePaths.ToArray(),
                Imports = Imports.ToArray(),
            };
    }
}
