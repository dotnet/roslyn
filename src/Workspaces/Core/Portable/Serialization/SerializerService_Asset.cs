// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
    private static void SerializeSourceText(SerializableSourceText text, ObjectWriter writer, SolutionReplicationContext context, CancellationToken cancellationToken)
    {
        text.Serialize(writer, context, cancellationToken);
    }

    private SourceText DeserializeSourceText(ObjectReader reader, CancellationToken cancellationToken)
    {
        var serializableSourceText = SerializableSourceText.Deserialize(reader, _storageService, _textService, cancellationToken);
        return serializableSourceText.GetText(cancellationToken);
    }

    private async ValueTask SerializeCompilationOptionsAsync(CompilationOptions options, ObjectWriter writer, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var language = options.Language;

        // TODO: once compiler team adds ability to serialize compilation options to ObjectWriter directly, we won't need this.
        await writer.WriteStringAsync(language).ConfigureAwait(false);

        var service = GetOptionsSerializationService(language);
        service.WriteTo(options, writer, cancellationToken);
    }

    private async ValueTask<CompilationOptions> DeserializeCompilationOptionsAsync(ObjectReader reader, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var language = await reader.ReadStringAsync().ConfigureAwait(false);

        var service = GetOptionsSerializationService(language);
        return service.ReadCompilationOptionsFrom(reader, cancellationToken);
    }

    public async ValueTask SerializeParseOptionsAsync(ParseOptions options, ObjectWriter writer)
    {
        var language = options.Language;

        // TODO: once compiler team adds ability to serialize parse options to ObjectWriter directly, we won't need this.
        await writer.WriteStringAsync(language).ConfigureAwait(false);

        var service = GetOptionsSerializationService(language);
        service.WriteTo(options, writer);
    }

    private async ValueTask<ParseOptions> DeserializeParseOptionsAsync(ObjectReader reader, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var language = await reader.ReadStringAsync().ConfigureAwait(false);

        var service = GetOptionsSerializationService(language);
        return service.ReadParseOptionsFrom(reader, cancellationToken);
    }

    private static async ValueTask SerializeProjectReferenceAsync(ProjectReference reference, ObjectWriter writer, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        reference.ProjectId.WriteTo(writer);
        await writer.WriteValueAsync(reference.Aliases.ToArray()).ConfigureAwait(false);
        writer.WriteBoolean(reference.EmbedInteropTypes);
    }

    private static async ValueTask<ProjectReference> DeserializeProjectReferenceAsync(ObjectReader reader, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var projectId = ProjectId.ReadFrom(reader);
        var aliases = reader.ReadArray<string>();
        var embedInteropTypes = await reader.ReadBooleanAsync().ConfigureAwait(false);

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
