// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization
{
    internal static class SerializationExtensions
    {
        public static WellKnownSynchronizationKind GetWellKnownSynchronizationKind(this object value)
            => value switch
            {
                SolutionStateChecksums _ => WellKnownSynchronizationKind.SolutionState,
                ProjectStateChecksums _ => WellKnownSynchronizationKind.ProjectState,
                DocumentStateChecksums _ => WellKnownSynchronizationKind.DocumentState,
                ProjectChecksumCollection _ => WellKnownSynchronizationKind.Projects,
                DocumentChecksumCollection _ => WellKnownSynchronizationKind.Documents,
                TextDocumentChecksumCollection _ => WellKnownSynchronizationKind.TextDocuments,
                AnalyzerConfigDocumentChecksumCollection _ => WellKnownSynchronizationKind.AnalyzerConfigDocuments,
                ProjectReferenceChecksumCollection _ => WellKnownSynchronizationKind.ProjectReferences,
                MetadataReferenceChecksumCollection _ => WellKnownSynchronizationKind.MetadataReferences,
                AnalyzerReferenceChecksumCollection _ => WellKnownSynchronizationKind.AnalyzerReferences,
                SolutionInfo.SolutionAttributes _ => WellKnownSynchronizationKind.SolutionAttributes,
                ProjectInfo.ProjectAttributes _ => WellKnownSynchronizationKind.ProjectAttributes,
                DocumentInfo.DocumentAttributes _ => WellKnownSynchronizationKind.DocumentAttributes,
                CompilationOptions _ => WellKnownSynchronizationKind.CompilationOptions,
                ParseOptions _ => WellKnownSynchronizationKind.ParseOptions,
                ProjectReference _ => WellKnownSynchronizationKind.ProjectReference,
                MetadataReference _ => WellKnownSynchronizationKind.MetadataReference,
                AnalyzerReference _ => WellKnownSynchronizationKind.AnalyzerReference,
                TextDocumentState _ => WellKnownSynchronizationKind.RecoverableSourceText,
                SerializableSourceText _ => WellKnownSynchronizationKind.SerializableSourceText,
                SourceText _ => WellKnownSynchronizationKind.SourceText,
                OptionSet _ => WellKnownSynchronizationKind.OptionSet,
                _ => throw ExceptionUtilities.UnexpectedValue(value),
            };

        public static CompilationOptions FixUpCompilationOptions(this ProjectInfo.ProjectAttributes info, CompilationOptions compilationOptions)
        {
            return compilationOptions.WithXmlReferenceResolver(GetXmlResolver(info.FilePath))
                                     .WithStrongNameProvider(new DesktopStrongNameProvider(GetStrongNameKeyPaths(info)));
        }

        private static XmlFileResolver GetXmlResolver(string? filePath)
        {
            // Given filePath can be any arbitrary string project is created with.
            // for primary solution in host such as VSWorkspace, ETA or MSBuildWorkspace
            // filePath will point to actual file on disk, but in memory solultion, or
            // one from AdhocWorkspace and etc, FilePath can be a random string.
            // Make sure we return only if given filePath is in right form.
            if (!PathUtilities.IsAbsolute(filePath))
            {
                // xmlFileResolver can only deal with absolute path
                // return Default
                return XmlFileResolver.Default;
            }

            return new XmlFileResolver(PathUtilities.GetDirectoryName(filePath));
        }

        private static ImmutableArray<string> GetStrongNameKeyPaths(ProjectInfo.ProjectAttributes info)
        {
            // Given FilePath/OutputFilePath can be any arbitrary strings project is created with.
            // for primary solution in host such as VSWorkspace, ETA or MSBuildWorkspace
            // filePath will point to actual file on disk, but in memory solultion, or
            // one from AdhocWorkspace and etc, FilePath/OutputFilePath can be a random string.
            // Make sure we return only if given filePath is in right form.
            if (info.FilePath == null && info.OutputFilePath == null)
            {
                // return empty since that is what IDE does for this case
                // see AbstractProject.GetStrongNameKeyPaths
                return ImmutableArray<string>.Empty;
            }

            var builder = ArrayBuilder<string>.GetInstance();
            if (PathUtilities.IsAbsolute(info.FilePath))
            {
                // desktop strong name provider only knows how to deal with absolute path
                builder.Add(PathUtilities.GetDirectoryName(info.FilePath)!);
            }

            if (PathUtilities.IsAbsolute(info.OutputFilePath))
            {
                // desktop strong name provider only knows how to deal with absolute path
                builder.Add(PathUtilities.GetDirectoryName(info.OutputFilePath)!);
            }

            return builder.ToImmutableAndFree();
        }
    }
}
