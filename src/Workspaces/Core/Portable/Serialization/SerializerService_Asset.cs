// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Serialization;

/// <summary>
/// serialize and deserialize objects to stream.
/// some of these could be moved into actual object, but putting everything here is a bit easier to find I believe.
/// </summary>
internal partial class SerializerService
{
    private static void SerializeSourceText(SerializableSourceText text, ObjectWriter writer, CancellationToken cancellationToken)
    {
        text.Serialize(writer, cancellationToken);
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

        var language = reader.ReadRequiredString();

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

        var language = reader.ReadRequiredString();

        var service = GetOptionsSerializationService(language);
        return service.ReadParseOptionsFrom(reader, cancellationToken);
    }

    private static void SerializeProjectReference(ProjectReference reference, ObjectWriter writer)
    {
        reference.ProjectId.WriteTo(writer);
        writer.WriteArray(reference.Aliases, static (w, a) => w.WriteString(a));
        writer.WriteBoolean(reference.EmbedInteropTypes);
    }

    private static ProjectReference DeserializeProjectReference(ObjectReader reader, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var projectId = ProjectId.ReadFrom(reader);
        var aliases = reader.ReadArray(static r => r.ReadString());
        var embedInteropTypes = reader.ReadBoolean();

        return new ProjectReference(projectId, aliases.ToImmutableArrayOrEmpty(), embedInteropTypes);
    }

    private void SerializeMetadataReference(MetadataReference reference, ObjectWriter writer)
        => WriteMetadataReferenceTo(reference, writer);

    private MetadataReference DeserializeMetadataReference(ObjectReader reader)
        => ReadMetadataReferenceFrom(reader);

    private void SerializeAnalyzerReference(AnalyzerReference reference, ObjectWriter writer)
        => WriteAnalyzerReferenceTo(reference, writer);

    private AnalyzerReference DeserializeAnalyzerReference(ObjectReader reader)
        => ReadAnalyzerReferenceFrom(reader);
}
