// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Common.ProjectUtils
{
    public abstract class Identity
    {
        public string Name { get; protected set; }
    }

    public class Project : Identity
    {
        public Project(string name)
        {
            Name = name;
        }
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
}
