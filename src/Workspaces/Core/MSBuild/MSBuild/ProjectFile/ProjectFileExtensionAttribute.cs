// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;

namespace Microsoft.CodeAnalysis.MSBuild
{
    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    internal class ProjectFileExtensionAttribute : Attribute
    {
        public ProjectFileExtensionAttribute(string extension)
        {
            this.ProjectFileExtension = extension;
        }

        public string ProjectFileExtension { get; set; }
    }
}
