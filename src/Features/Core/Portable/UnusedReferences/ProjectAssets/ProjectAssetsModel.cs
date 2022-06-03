// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.UnusedReferences.ProjectAssets
{
    // These classes model enough of the version 3 project.assets.json file that we can
    // parse out the dependency tree and compilation assemblies that each reference brings
    // in to the project.

    internal class ProjectAssetsFile
    {
        public int Version { get; set; }
        public Dictionary<string, Dictionary<string, ProjectAssetsTargetLibrary>>? Targets { get; set; }
        public Dictionary<string, ProjectAssetsLibrary>? Libraries { get; set; }
        public Dictionary<string, List<string>>? ProjectFileDependencyGroups { get; set; }
        public ProjectAssetsProject? Project { get; set; }
    }

    internal class ProjectAssetsTargetLibrary
    {
        public string? Type { get; set; }
        public Dictionary<string, string>? Dependencies { get; set; }
        public Dictionary<string, ProjectAssetsTargetLibraryCompile>? Compile { get; set; }
    }

    internal class ProjectAssetsTargetLibraryCompile
    {

    }

    internal class ProjectAssetsLibrary
    {
        public string? Path { get; set; }
    }

    internal class ProjectAssetsProject
    {
        public ProjectAssetsProjectRestore? Restore { get; set; }
        public Dictionary<string, ProjectAssetsProjectFramework>? Frameworks { get; set; }
    }

    internal class ProjectAssetsProjectRestore
    {
        public string? ProjectPath { get; set; }
        public string? PackagesPath { get; set; }
    }

    internal class ProjectAssetsProjectFramework
    {
        public string? TargetAlias { get; set; }
        public Dictionary<string, ProjectAssetsProjectFrameworkDependency>? Dependencies { get; set; }
    }

    internal class ProjectAssetsProjectFrameworkDependency
    {
        public bool AutoReferenced { get; set; }
    }
}
