// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils
{
    public abstract class Identity
    {
        public string Name { get; protected set; }
    }

    public class Project : Identity
    {
        public Project(string name, string relativePath = null)
        {
            Name = name;
            RelativePath = relativePath;
        }

        public string RelativePath { get; }
    }

    public class ProjectReference : Identity
    {
        public ProjectReference(string name)
        {
            Name = name;
        }
    }

    public class AssemblyReference : Identity
    {
        public AssemblyReference(string name)
        {
            Name = name;
        }
    }

    public class PackageReference : Identity
    {
        public string Version { get; }

        public PackageReference(string name, string version)
        {
            Name = name;
            Version = version;
        }
    }
}
