// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    internal static class Extensions
    {
        public static T[] ReadArray<T>(this ObjectReader reader)
        {
            return (T[])reader.ReadValue();
        }

        public static WellKnownSynchronizationKinds GetWellKnownSynchronizationKind(this object value)
        {
            switch (value)
            {
                case SolutionStateChecksums _: return WellKnownSynchronizationKinds.SolutionState;
                case ProjectStateChecksums _: return WellKnownSynchronizationKinds.ProjectState;
                case DocumentStateChecksums _: return WellKnownSynchronizationKinds.DocumentState;
                case ProjectChecksumCollection _: return WellKnownSynchronizationKinds.Projects;
                case DocumentChecksumCollection _: return WellKnownSynchronizationKinds.Documents;
                case TextDocumentChecksumCollection _: return WellKnownSynchronizationKinds.TextDocuments;
                case ProjectReferenceChecksumCollection _: return WellKnownSynchronizationKinds.ProjectReferences;
                case MetadataReferenceChecksumCollection _: return WellKnownSynchronizationKinds.MetadataReferences;
                case AnalyzerReferenceChecksumCollection _: return WellKnownSynchronizationKinds.AnalyzerReferences;
                case SolutionInfo.SolutionAttributes _: return WellKnownSynchronizationKinds.SolutionAttributes;
                case ProjectInfo.ProjectAttributes _: return WellKnownSynchronizationKinds.ProjectAttributes;
                case DocumentInfo.DocumentAttributes _: return WellKnownSynchronizationKinds.DocumentAttributes;
                case CompilationOptions _: return WellKnownSynchronizationKinds.CompilationOptions;
                case ParseOptions _: return WellKnownSynchronizationKinds.ParseOptions;
                case ProjectReference _: return WellKnownSynchronizationKinds.ProjectReference;
                case MetadataReference _: return WellKnownSynchronizationKinds.MetadataReference;
                case AnalyzerReference _: return WellKnownSynchronizationKinds.AnalyzerReference;
                case TextDocumentState _: return WellKnownSynchronizationKinds.RecoverableSourceText;
                case SourceText _: return WellKnownSynchronizationKinds.SourceText;
                case OptionSet _: return WellKnownSynchronizationKinds.OptionSet;
            }

            throw ExceptionUtilities.UnexpectedValue(value);
        }
    }
}
