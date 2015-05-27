// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.MSBuild
{
    /// <summary>
    /// Represents a reference to another project file.
    /// </summary>
    [Serializable]
    internal sealed class ProjectAnalyzerReference
    {
        /// <summary>
        /// </summary>
        public string Path { get; }

        public string Display { get; }

        public ProjectAnalyzerReference(string path, string display)
        {
            this.Path = path;
            this.Display = display;
        }
    }
}