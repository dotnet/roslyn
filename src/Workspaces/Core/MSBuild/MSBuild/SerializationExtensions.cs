// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Options;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.MSBuild
{
    internal static class SerializationExtensions
    {
        public static ProjectStateChecksums GetCheckSum(this ProjectInfo projectInfo, ISerializerService serializer)
        {
            var infoChecksum = projectInfo.Attributes.GetCheckSum();
            var compilationChecksum = Checksum.Create(WellKnownSynchronizationKind.CompilationOptions, projectInfo.CompilationOptions, serializer);
            var parseOptionsChecksum = Checksum.Create(WellKnownSynchronizationKind.ParseOptions, projectInfo.ParseOptions, serializer);

            var projectReferenceChecksums = new ProjectReferenceChecksumCollection(projectInfo.ProjectReferences.SelectAsArray(pr => serializer.CreateChecksum(pr, default)).ToArray());
            var metadataReferenceChecksums = new MetadataReferenceChecksumCollection(projectInfo.MetadataReferences.SelectAsArray(mr => serializer.CreateChecksum(mr, default)).ToArray());
            var analyzerReferenceChecksums = new AnalyzerReferenceChecksumCollection(projectInfo.AnalyzerReferences.SelectAsArray(ar => serializer.CreateChecksum(ar, default)).ToArray());

            var documentChecksums = new DocumentChecksumCollection(projectInfo.Documents.SelectAsArray(di => di.GetCheckSum()).ToArray());
            var additionalChecksums = new TextDocumentChecksumCollection(projectInfo.AdditionalDocuments.SelectAsArray(di => di.GetCheckSum()).ToArray());
            var analyzerConfigDocumentChecksums = new AnalyzerConfigDocumentChecksumCollection(projectInfo.AnalyzerConfigDocuments.SelectAsArray(di => di.GetCheckSum()).ToArray());

            return new ProjectStateChecksums(infoChecksum,
                                             compilationChecksum,
                                             parseOptionsChecksum,
                                             documentChecksums,
                                             projectReferenceChecksums,
                                             metadataReferenceChecksums,
                                             analyzerReferenceChecksums,
                                             additionalChecksums,
                                             analyzerConfigDocumentChecksums);
        }

        public static Checksum GetCheckSum(this DocumentInfo documentInfo)
        {
            return documentInfo.Attributes.GetCheckSum();
        }

        public static Checksum GetCheckSum<T>(this T @object)
            where T : IObjectWritable
        {
            return @object switch
            {
                ProjectChecksumCollection x => Checksum.Create(WellKnownSynchronizationKind.Projects, x),
                DocumentChecksumCollection x => Checksum.Create(WellKnownSynchronizationKind.Documents, x),
                TextDocumentChecksumCollection x => Checksum.Create(WellKnownSynchronizationKind.TextDocuments, x),
                AnalyzerConfigDocumentChecksumCollection x => Checksum.Create(WellKnownSynchronizationKind.AnalyzerConfigDocuments, x),
                ProjectReferenceChecksumCollection x => Checksum.Create(WellKnownSynchronizationKind.ProjectReferences, x),
                MetadataReferenceChecksumCollection x => Checksum.Create(WellKnownSynchronizationKind.MetadataReferences, x),
                AnalyzerReferenceChecksumCollection x => Checksum.Create(WellKnownSynchronizationKind.AnalyzerReferences, x),
                SolutionInfo.SolutionAttributes x => Checksum.Create(WellKnownSynchronizationKind.SolutionAttributes, x),
                ProjectInfo.ProjectAttributes x => Checksum.Create(WellKnownSynchronizationKind.ProjectAttributes, x),
                DocumentInfo.DocumentAttributes x => Checksum.Create(WellKnownSynchronizationKind.DocumentAttributes, x),
                _ => throw new InvalidOperationException()
            };
        }
    }
}
