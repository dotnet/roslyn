// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils
{
    public abstract class Identity
    {
        protected Identity(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }

    public class Project : Identity
    {
        public Project(string name, string projectExtension = ".csproj", string? relativePath = null)
            : base(name)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                RelativePath = Path.Combine(name, name + projectExtension);
            }
            else
            {
                RelativePath = relativePath;
            }
        }

        /// <summary>
        /// This path is relative to the Solution file. Default value is set to ProjectName\ProjectName.csproj
        /// </summary>
        public string? RelativePath { get; }
    }

    public class ProjectReference : Identity
    {
        public ProjectReference(string name)
            : base(name)
        {
        }
    }

    public class AssemblyReference : Identity
    {
        public AssemblyReference(string name)
            : base(name)
        {
        }
    }

    public class PackageReference : Identity
    {
        public string Version { get; }

        public PackageReference(string name, string version)
            : base(name)
        {
            Version = version;
        }
    }
}
