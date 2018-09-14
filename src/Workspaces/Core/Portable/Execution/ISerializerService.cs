// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization
{
    internal interface ISerializerService : IWorkspaceService
    {
        void Serialize(object value, ObjectWriter writer, CancellationToken cancellationToken);

        void SerializeSourceText(ITemporaryStorageWithName storage, SourceText text, ObjectWriter writer, CancellationToken cancellationToken);

        void SerializeCompilationOptions(CompilationOptions options, ObjectWriter writer, CancellationToken cancellationToken);

        void SerializeParseOptions(ParseOptions options, ObjectWriter writer, CancellationToken cancellationToken);

        void SerializeProjectReference(ProjectReference reference, ObjectWriter writer, CancellationToken cancellationToken);

        void SerializeMetadataReference(MetadataReference reference, ObjectWriter writer, CancellationToken cancellationToken);

        void SerializeAnalyzerReference(AnalyzerReference reference, ObjectWriter writer, bool usePathFromAssembly, CancellationToken cancellationToken);

        void SerializeChecksumWithChildren(ChecksumWithChildren checksums, ObjectWriter writer, CancellationToken cancellationToken);

        void SerializeOptionSet(OptionSet options, string language, ObjectWriter writer, CancellationToken cancellationToken);

        T Deserialize<T>(WellKnownSynchronizationKind kind, ObjectReader reader, CancellationToken cancellationToken);

        Checksum CreateChecksum(object value, CancellationToken cancellationToken);
    }
}
