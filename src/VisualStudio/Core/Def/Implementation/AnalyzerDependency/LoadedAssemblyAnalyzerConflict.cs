// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using System.Diagnostics;

namespace Microsoft.VisualStudio.LanguageServices.Implementation
{
    internal sealed class LoadedAssemblyAnalyzerConflict
    {
        public LoadedAssemblyAnalyzerConflict(AssemblyIdentity loadedAssemblyIdentity, AssemblyIdentity conflictingAnalyzerIdentity, string conflictingAnalyzerFilePath)
        {
            Debug.Assert(loadedAssemblyIdentity != null);
            Debug.Assert(conflictingAnalyzerIdentity != null);            
            Debug.Assert(conflictingAnalyzerFilePath != null);

            LoadedAssemblyIdentity = loadedAssemblyIdentity;
            ConflictingAnalyzerIdentity = conflictingAnalyzerIdentity;
            ConflictingAnalyzerFilePath = conflictingAnalyzerFilePath;
        }

        public AssemblyIdentity LoadedAssemblyIdentity { get; }
        public AssemblyIdentity ConflictingAnalyzerIdentity { get; }
        public string ConflictingAnalyzerFilePath { get; }        
    }
}
