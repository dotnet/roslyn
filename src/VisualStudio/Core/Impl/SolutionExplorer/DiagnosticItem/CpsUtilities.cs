// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

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
    public static string? ExtractAnalyzerFilePath(string projectDirectoryPath, string analyzerNodeCanonicalName)
    {
        // The canonical name may or may not start with the path to the project's directory.
        if (!projectDirectoryPath.EndsWith("\\"))
        {
            projectDirectoryPath += '\\';
        }

        if (analyzerNodeCanonicalName.StartsWith(projectDirectoryPath, StringComparison.OrdinalIgnoreCase))
        {
            // Extract the rest of the string
            analyzerNodeCanonicalName = analyzerNodeCanonicalName[projectDirectoryPath.Length..];
        }

        // Find the slash after the target framework
        var backslashIndex = analyzerNodeCanonicalName.IndexOf('\\');
        if (backslashIndex < 0)
        {
            return null;
        }

        // If the path does not contain "analyzerdependency\" immediately after the first slash, it
        // is a newer form of the analyzer tree item's file path (VS16.7) which requires no processing.
        //
        // It is theoretically possible that this incorrectly identifies an analyzer assembly
        // defined under "c:\analyzerdependency\..." as data in the old format, however this is very
        // unlikely. The side effect of such a problem is that analyzer's diagnostics would not
        // populate in the tree.
        if (analyzerNodeCanonicalName.IndexOf(@"analyzerdependency\", backslashIndex + 1, @"analyzerdependency\".Length, StringComparison.OrdinalIgnoreCase) != backslashIndex + 1)
        {
            return analyzerNodeCanonicalName;
        }

        // Find the slash after "analyzerdependency"
        backslashIndex = analyzerNodeCanonicalName.IndexOf('\\', backslashIndex + 1);
        if (backslashIndex < 0)
        {
            return null;
        }

        // The rest of the string is the path.
        return analyzerNodeCanonicalName[(backslashIndex + 1)..];
    }
}
