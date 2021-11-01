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
using System.IO;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.MSBuild
{
    internal static class SerializationExtensions
    {
        public static ProjectStateChecksums GetChecksum(this ProjectInfo projectInfo, ISerializerService serializer)
        {
            var infoChecksum = serializer.CreateChecksum(projectInfo.Attributes, default);
            var compilationOptionsChecksum = ChecksumCache.GetOrCreate(projectInfo.CompilationOptions!, _ => serializer.CreateChecksum(projectInfo.CompilationOptions!, default));

            var parseOptionsChecksum = serializer.CreateChecksum(projectInfo.ParseOptions!, default);

            var projectReferenceChecksums = ChecksumCache.GetOrCreate<ChecksumCollection>(projectInfo.ProjectReferences, _ => new ChecksumCollection(projectInfo.ProjectReferences.Select(r => serializer.CreateChecksum(r, default)).ToArray()));
            var metadataReferenceChecksums = ChecksumCache.GetOrCreate<ChecksumCollection>(projectInfo.MetadataReferences, _ => new ChecksumCollection(projectInfo.MetadataReferences.Select(r => serializer.CreateChecksum(r, default)).ToArray()));
            var analyzerReferenceChecksums = ChecksumCache.GetOrCreate<ChecksumCollection>(projectInfo.AnalyzerReferences, _ => new ChecksumCollection(projectInfo.AnalyzerReferences.Select(r => serializer.CreateChecksum(r, default)).ToArray()));

            var documentChecksums = GetChecksumCollection(projectInfo.Documents, serializer);
            var additionalChecksums = GetChecksumCollection(projectInfo.AdditionalDocuments, serializer);
            var analyzerConfigDocumentChecksums = GetChecksumCollection(projectInfo.AnalyzerConfigDocuments, serializer);

            return new ProjectStateChecksums(infoChecksum,
                                             compilationOptionsChecksum,
                                             parseOptionsChecksum,
                                             documentChecksums,
                                             projectReferenceChecksums,
                                             metadataReferenceChecksums,
                                             analyzerReferenceChecksums,
                                             additionalChecksums,
                                             analyzerConfigDocumentChecksums);

            static ChecksumCollection GetChecksumCollection(IReadOnlyList<DocumentInfo> documents, ISerializerService serializer)
                => new(documents.SelectAsArray(documentInfo => GetDocumentInfoChecksum(documentInfo, serializer)).ToArray());

            static Checksum GetDocumentInfoChecksum(DocumentInfo documentInfo, ISerializerService serializer)
            {
                var infoChecksum = serializer.CreateChecksum(documentInfo.Attributes, default);
                var sourceText = SourceText.From(File.OpenRead(documentInfo.FilePath));
                var textChecksum = serializer.CreateChecksum(sourceText, default);
                return new DocumentStateChecksums(infoChecksum, textChecksum).Checksum;
            }
        }
    }
}
