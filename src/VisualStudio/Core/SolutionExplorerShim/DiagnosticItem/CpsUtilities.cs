// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer
{
    internal static class CpsUtilities
    {
        /// <summary>
        /// Given the canonical name of a node representing an analyzer assembly in the
        /// CPS-based project system extracts out the full path to the assembly.
        /// </summary>
        /// <param name="projectDirectoryPath">The full path to the project directory</param>
        /// <param name="analyzerNodeCanonicalName">The canonical name of the analyzer node in the hierarchy</param>
        /// <returns>The full path to the analyzer assembly on disk, or null if <paramref name="analyzerNodeCanonicalName"/>
        /// cannot be parsed.</returns>
        /// <remarks>
        /// The canonical name takes the following form:
        /// 
        ///   [{path to project directory}\]{target framework}\analyzerdependency\{path to assembly}
        ///   
        /// e.g.:
        /// 
        ///   C:\projects\solutions\MyProj\netstandard2.0\analyzerdependency\C:\users\me\.packages\somePackage\lib\someAnalyzer.dll
        ///   
        /// This method exists solely to extract out the "path to assembly" part, i.e.
        /// "C:\users\me\.packages\somePackage\lib\someAnalyzer.dll". We don't need the
        /// other parts.
        /// 
        /// Note that the path to the project directory is optional.
        /// </remarks>
        public static string ExtractAnalyzerFilePath(string projectDirectoryPath, string analyzerNodeCanonicalName)
        {
            // The canonical name may or may not start with the path to the project's directory.
            if (analyzerNodeCanonicalName.StartsWith(projectDirectoryPath, StringComparison.OrdinalIgnoreCase))
            {
                // Extract the rest of the string, taking into account the "\" separating the directory
                // path from the rest of the canonical name
                analyzerNodeCanonicalName = analyzerNodeCanonicalName.Substring(projectDirectoryPath.Length + 1);
            }

            // Find the slash after the target framework
            var backslashIndex = analyzerNodeCanonicalName.IndexOf('\\');
            if (backslashIndex < 0)
            {
                return null;
            }

            // Find the slash after "analyzerdependency"
            backslashIndex = analyzerNodeCanonicalName.IndexOf('\\', backslashIndex + 1);
            if (backslashIndex < 0)
            {
                return null;
            }

            // The rest of the string is the path.
            return analyzerNodeCanonicalName.Substring(backslashIndex + 1);
        }
    }
}
