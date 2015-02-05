// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal sealed class AnalyzerDependencyConflict
    {
        public AnalyzerDependencyConflict(string dependencyFilePath1, string dependencyFilePath2, string analyzerFilePath1, string analyzerFilePath2)
        {
            DependencyFilePath1 = dependencyFilePath1;
            DependencyFilePath2 = dependencyFilePath2;
            AnalyzerFilePath1 = analyzerFilePath1;
            AnalyzerFilePath2 = analyzerFilePath2;
        }

        public string DependencyFilePath1 { get; }
        public string DependencyFilePath2 { get; }
        public string AnalyzerFilePath1 { get; }
        public string AnalyzerFilePath2 { get; }
    }
}
