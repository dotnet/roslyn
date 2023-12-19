// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization;

/// <summary>
/// serialize and deserialize objects to stream.
/// some of these could be moved into actual object, but putting everything here is a bit easier to find I believe.
/// </summary>
internal partial class SerializerService
{
    public void SerializeSourceText(SourceText text, ObjectWriter writer, CancellationToken cancellationToken)
    {
        writer.WriteInt32((int)text.ChecksumAlgorithm);
        writer.WriteEncoding(text.Encoding);
        text.WriteTo(writer, cancellationToken);
    }

    private SourceText DeserializeSourceText(ObjectReader reader, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var checksumAlgorithm = (SourceHashAlgorithm)reader.ReadInt32();
        var encoding = (Encoding)reader.ReadValue();

        return SourceTextExtensions.ReadFrom(_textService, reader, encoding, checksumAlgorithm, cancellationToken);
    }

    private void SerializeCompilationOptions(CompilationOptions options, ObjectWriter writer, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var language = options.Language;

        // TODO: once compiler team adds ability to serialize compilation options to ObjectWriter directly, we won't need this.
        writer.WriteString(language);

        var service = GetOptionsSerializationService(language);
        service.WriteTo(options, writer, cancellationToken);
    }

    private CompilationOptions DeserializeCompilationOptions(ObjectReader reader, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var language = reader.ReadString();

        var service = GetOptionsSerializationService(language);
        return service.ReadCompilationOptionsFrom(reader, cancellationToken);
    }

    public void SerializeParseOptions(ParseOptions options, ObjectWriter writer)
    {
        var language = options.Language;

        // TODO: once compiler team adds ability to serialize parse options to ObjectWriter directly, we won't need this.
        writer.WriteString(language);

        var service = GetOptionsSerializationService(language);
        service.WriteTo(options, writer);
    }

    private ParseOptions DeserializeParseOptions(ObjectReader reader, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var language = reader.ReadString();

        var service = GetOptionsSerializationService(language);
        return service.ReadParseOptionsFrom(reader, cancellationToken);
    }

    private static void SerializeProjectReference(ProjectReference reference, ObjectWriter writer, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        reference.ProjectId.WriteTo(writer);
        writer.WriteValue(reference.Aliases.ToArray());
        writer.WriteBoolean(reference.EmbedInteropTypes);
    }

    private static ProjectReference DeserializeProjectReference(ObjectReader reader, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var projectId = ProjectId.ReadFrom(reader);
        var aliases = reader.ReadArray<string>();
        var embedInteropTypes = reader.ReadBoolean();

        return new ProjectReference(projectId, aliases.ToImmutableArrayOrEmpty(), embedInteropTypes);
    }

    private void SerializeMetadataReference(MetadataReference reference, ObjectWriter writer, SolutionReplicationContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        WriteMetadataReferenceTo(reference, writer, context, cancellationToken);
    }

    private MetadataReference DeserializeMetadataReference(ObjectReader reader, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ReadMetadataReferenceFrom(reader, cancellationToken);
    }

    private void SerializeAnalyzerReference(AnalyzerReference reference, ObjectWriter writer, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        WriteAnalyzerReferenceTo(reference, writer, cancellationToken);
    }

    private AnalyzerReference DeserializeAnalyzerReference(ObjectReader reader, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ReadAnalyzerReferenceFrom(reader, cancellationToken);
    }
}
