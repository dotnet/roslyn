// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
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
                case AnalyzerConfigDocumentChecksumCollection _: return WellKnownSynchronizationKind.AnalyzerConfigDocuments;
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

        public static CompilationOptions FixUpCompilationOptions(this ProjectInfo.ProjectAttributes info, CompilationOptions compilationOptions)
        {
            return compilationOptions.WithXmlReferenceResolver(GetXmlResolver(info.FilePath))
                                     .WithStrongNameProvider(new DesktopStrongNameProvider(GetStrongNameKeyPaths(info)));
        }

        private static XmlFileResolver GetXmlResolver(string filePath)
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
            if (info.FilePath != null && PathUtilities.IsAbsolute(info.FilePath))
            {
                // desktop strong name provider only knows how to deal with absolute path
                builder.Add(PathUtilities.GetDirectoryName(info.FilePath));
            }

            if (info.OutputFilePath != null && PathUtilities.IsAbsolute(info.OutputFilePath))
            {
                // desktop strong name provider only knows how to deal with absolute path
                builder.Add(PathUtilities.GetDirectoryName(info.OutputFilePath));
            }

            return builder.ToImmutableAndFree();
        }

        public static async Task<List<T>> CreateCollectionAsync<T>(this IAssetProvider assetProvider, ChecksumCollection collections, CancellationToken cancellationToken)
        {
            var assets = new List<T>();

            foreach (var checksum in collections)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var asset = await assetProvider.GetAssetAsync<T>(checksum, cancellationToken).ConfigureAwait(false);
                assets.Add(asset);
            }

            return assets;
        }
    }
}
