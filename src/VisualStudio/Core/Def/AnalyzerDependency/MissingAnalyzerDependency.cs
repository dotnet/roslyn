// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis;
using System.Diagnostics;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal sealed class MissingAnalyzerDependency
    {
        public MissingAnalyzerDependency(string analyzerPath, AssemblyIdentity dependencyIdentity)
        {
            Debug.Assert(analyzerPath != null);
            Debug.Assert(dependencyIdentity != null);

            AnalyzerPath = analyzerPath;
            DependencyIdentity = dependencyIdentity;
        }

        public string AnalyzerPath { get; }
        public AssemblyIdentity DependencyIdentity { get; }
    }
}
