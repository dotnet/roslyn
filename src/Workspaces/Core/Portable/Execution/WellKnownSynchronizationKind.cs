// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Reflection;

namespace Microsoft.CodeAnalysis.Serialization
{
    // TODO: Kind might not actually needed. see whether we can get rid of this
    internal enum WellKnownSynchronizationKind
    {
        Null,

        SolutionState,
        ProjectState,
        DocumentState,

        Projects,
        Documents,
        TextDocuments,
        ProjectReferences,
        MetadataReferences,
        AnalyzerReferences,

        SolutionAttributes,
        ProjectAttributes,
        DocumentAttributes,

        CompilationOptions,
        ParseOptions,
        ProjectReference,
        MetadataReference,
        AnalyzerReference,
        SourceText,
        OptionSet,

        RecoverableSourceText,

        //

        SyntaxTreeIndex,
        SymbolTreeInfo,

        ProjectReferenceChecksumCollection,
        MetadataReferenceChecksumCollection,
        AnalyzerReferenceChecksumCollection,
        TextDocumentChecksumCollection,
        DocumentChecksumCollection,
        ProjectChecksumCollection,
        SolutionStateChecksums,
        ProjectStateChecksums,
        DocumentStateChecksums,
    }

    internal static class WellKnownSynchronizationKindExtensions
    {
        private static readonly string[] s_strings;

        static WellKnownSynchronizationKindExtensions()
        {
            var fields = typeof(WellKnownSynchronizationKind).GetTypeInfo().DeclaredFields.Where(f => f.IsStatic);

            var maxValue = fields.Max(f => (int)f.GetValue(null));
            s_strings = new string[maxValue + 1];

            foreach (var field in fields)
            {
                var value = (int)field.GetValue(null);
                s_strings[value] = field.Name;
            }
        }

        public static string ToStringFast(this WellKnownSynchronizationKind kind)
            => s_strings[(int)kind];
    }
}
