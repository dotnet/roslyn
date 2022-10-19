// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
