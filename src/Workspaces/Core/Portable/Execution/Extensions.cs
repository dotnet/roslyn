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

        public static WellKnownSynchronizationKind GetWellKnownSynchronizationKind(this object value)
        {
            switch (value)
            {
                case SolutionStateChecksums _: return WellKnownSynchronizationKind.SolutionState;
                case ProjectStateChecksums _: return WellKnownSynchronizationKind.ProjectState;
                case DocumentStateChecksums _: return WellKnownSynchronizationKind.DocumentState;
                case ProjectChecksumCollection _: return WellKnownSynchronizationKind.Projects;
                case DocumentChecksumCollection _: return WellKnownSynchronizationKind.Documents;
                case TextDocumentChecksumCollection _: return WellKnownSynchronizationKind.TextDocuments;
                case ProjectReferenceChecksumCollection _: return WellKnownSynchronizationKind.ProjectReferences;
                case MetadataReferenceChecksumCollection _: return WellKnownSynchronizationKind.MetadataReferences;
                case AnalyzerReferenceChecksumCollection _: return WellKnownSynchronizationKind.AnalyzerReferences;
                case SolutionInfo.SolutionAttributes _: return WellKnownSynchronizationKind.SolutionAttributes;
                case ProjectInfo.ProjectAttributes _: return WellKnownSynchronizationKind.ProjectAttributes;
                case DocumentInfo.DocumentAttributes _: return WellKnownSynchronizationKind.DocumentAttributes;
                case CompilationOptions _: return WellKnownSynchronizationKind.CompilationOptions;
                case ParseOptions _: return WellKnownSynchronizationKind.ParseOptions;
                case ProjectReference _: return WellKnownSynchronizationKind.ProjectReference;
                case MetadataReference _: return WellKnownSynchronizationKind.MetadataReference;
                case AnalyzerReference _: return WellKnownSynchronizationKind.AnalyzerReference;
                case TextDocumentState _: return WellKnownSynchronizationKind.RecoverableSourceText;
                case SourceText _: return WellKnownSynchronizationKind.SourceText;
                case OptionSet _: return WellKnownSynchronizationKind.OptionSet;
            }

            throw ExceptionUtilities.UnexpectedValue(value);
        }
    }
}
