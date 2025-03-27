// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class CommonCompiler
    {
        /// <summary>
        /// Looks for metadata references among the assembly file references given to the compilation when constructed.
        /// When scripts are included into a project we don't want #r's to reference other assemblies than those 
        /// specified explicitly in the project references.
        /// </summary>
        internal sealed class CompilerRelativePathResolver : RelativePathResolver
        {
            internal ICommonCompilerFileSystem FileSystem { get; }

            internal CompilerRelativePathResolver(ICommonCompilerFileSystem fileSystem, ImmutableArray<string> searchPaths, string? baseDirectory)
                : base(searchPaths, baseDirectory)
            {
                FileSystem = fileSystem;
            }

            protected override bool FileExists(string fullPath) => FileSystem.FileExists(fullPath);
        }
    }
}
