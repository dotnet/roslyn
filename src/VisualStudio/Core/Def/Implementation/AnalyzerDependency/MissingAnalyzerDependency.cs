// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
