// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using System.Diagnostics;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal sealed class AnalyzerDependencyConflict
    {
        public AnalyzerDependencyConflict(AssemblyIdentity identity, string analyzerFilePath1, string analyzerFilePath2)
        {
            Debug.Assert(identity != null);
            Debug.Assert(analyzerFilePath1 != null);
            Debug.Assert(analyzerFilePath2 != null);

            Identity = identity;
            AnalyzerFilePath1 = analyzerFilePath1;
            AnalyzerFilePath2 = analyzerFilePath2;
        }

        public string AnalyzerFilePath1 { get; }
        public string AnalyzerFilePath2 { get; }
        public AssemblyIdentity Identity { get; }
    }
}
