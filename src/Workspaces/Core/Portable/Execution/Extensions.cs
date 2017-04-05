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

        public static string GetWellKnownSynchronizationKind(this object value)
        {
            if (value is SolutionStateChecksums)
            {
                return WellKnownSynchronizationKinds.SolutionState;
            }

            if (value is ProjectStateChecksums)
            {
                return WellKnownSynchronizationKinds.ProjectState;
            }

            if (value is DocumentStateChecksums)
            {
                return WellKnownSynchronizationKinds.DocumentState;
            }

            if (value is ProjectChecksumCollection)
            {
                return WellKnownSynchronizationKinds.Projects;
            }

            if (value is DocumentChecksumCollection)
            {
                return WellKnownSynchronizationKinds.Documents;
            }

            if (value is TextDocumentChecksumCollection)
            {
                return WellKnownSynchronizationKinds.TextDocuments;
            }

            if (value is ProjectReferenceChecksumCollection)
            {
                return WellKnownSynchronizationKinds.ProjectReferences;
            }

            if (value is MetadataReferenceChecksumCollection)
            {
                return WellKnownSynchronizationKinds.MetadataReferences;
            }

            if (value is AnalyzerReferenceChecksumCollection)
            {
                return WellKnownSynchronizationKinds.AnalyzerReferences;
            }

            if (value is SolutionInfo.SolutionAttributes)
            {
                return WellKnownSynchronizationKinds.SolutionAttributes;
            }

            if (value is ProjectInfo.ProjectAttributes)
            {
                return WellKnownSynchronizationKinds.ProjectAttributes;
            }

            if (value is DocumentInfo.DocumentAttributes)
            {
                return WellKnownSynchronizationKinds.DocumentAttributes;
            }

            if (value is CompilationOptions)
            {
                return WellKnownSynchronizationKinds.CompilationOptions;
            }

            if (value is ParseOptions)
            {
                return WellKnownSynchronizationKinds.ParseOptions;
            }

            if (value is ProjectReference)
            {
                return WellKnownSynchronizationKinds.ProjectReference;
            }

            if (value is MetadataReference)
            {
                return WellKnownSynchronizationKinds.MetadataReference;
            }

            if (value is AnalyzerReference)
            {
                return WellKnownSynchronizationKinds.AnalyzerReference;
            }

            if (value is TextDocumentState)
            {
                return WellKnownSynchronizationKinds.RecoverableSourceText;
            }

            if (value is SourceText)
            {
                return WellKnownSynchronizationKinds.SourceText;
            }

            if (value is OptionSet)
            {
                return WellKnownSynchronizationKinds.OptionSet;
            }

            throw ExceptionUtilities.UnexpectedValue(value);
        }
    }
}
