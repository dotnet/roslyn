// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
    internal static class ReferencePathUtilities
    {
        public static bool TryGetReferenceFilePath(string filePath, out string referenceFilePath)
        {
            // TODO(DustinCa): This is a workaround and we'll need to update this to handle getting the
            // correct reference assembly for different framework versions and profiles. We can use
            // the handy ToolLocationHelper from Microsoft.Build.Utilities.v4.5.dll

            var assemblyName = Path.GetFileName(filePath);

            // NOTE: Don't use the Path.HasExtension() and Path.ChangeExtension() helpers because
            // an assembly might have a dotted name like 'System.Core'.
            var extension = Path.GetExtension(assemblyName);
            if (!string.Equals(extension, ".dll", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase))
            {
                assemblyName += ".dll";
            }

            foreach (var referenceAssemblyPath in GetReferencePaths())
            {
                var referenceAssembly = Path.Combine(referenceAssemblyPath, assemblyName);
                if (File.Exists(referenceAssembly))
                {
                    referenceFilePath = referenceAssembly;
                    return true;
                }
            }

            referenceFilePath = null;
            return false;
        }

        private static IEnumerable<string> GetFrameworkPaths()
        {
            ////            Concat(Path.GetDirectoryName(typeof(Microsoft.CSharp.RuntimeHelpers.SessionHelpers).Assembly.Location)).
            return GlobalAssemblyCacheLocation.RootLocations.Concat(RuntimeEnvironment.GetRuntimeDirectory());
        }

        public static IEnumerable<string> GetReferencePaths()
        {
            // TODO:
            // WORKAROUND: properly enumerate them
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5");
            yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0");
        }

        public static bool PartOfFrameworkOrReferencePaths(string filePath)
        {
            if (!PathUtilities.IsAbsolute(filePath))
            {
                return false;
            }

            var directory = Path.GetDirectoryName(filePath);

            var frameworkOrReferencePaths = GetReferencePaths().Concat(GetFrameworkPaths()).Select(FileUtilities.NormalizeDirectoryPath);
            return frameworkOrReferencePaths.Any(dir => directory.StartsWith(dir, StringComparison.OrdinalIgnoreCase));
        }
    }
}
