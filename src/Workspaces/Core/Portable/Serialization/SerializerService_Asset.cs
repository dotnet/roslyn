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
    private static async ValueTask SerializeSourceTextAsync(SerializableSourceText text, ObjectWriter writer, SolutionReplicationContext context, CancellationToken cancellationToken)
    {
        await text.SerializeAsync(writer, context, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<SourceText> DeserializeSourceTextAsync(ObjectReader reader, CancellationToken cancellationToken)
    {
        var serializableSourceText = await SerializableSourceText.DeserializeAsync(
            reader, _storageService, _textService, cancellationToken).ConfigureAwait(false);
        return serializableSourceText.GetText(cancellationToken);
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

    private async ValueTask<CompilationOptions> DeserializeCompilationOptionsAsync(ObjectReader reader, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var language = await reader.ReadStringAsync().ConfigureAwait(false);

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
        writer.WriteArray(reference.Aliases, static (w, a) => w.WriteString(a));
        writer.WriteBoolean(reference.EmbedInteropTypes);
    }

    private static async ValueTask<ProjectReference> DeserializeProjectReferenceAsync(ObjectReader reader, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var projectId = ProjectId.ReadFrom(reader);
        var aliases = await reader.ReadArrayAsync(static r => r.ReadStringAsync()).ConfigureAwait(false);
        var embedInteropTypes = await reader.ReadBooleanAsync().ConfigureAwait(false);

        return new ProjectReference(projectId, aliases.ToImmutableArrayOrEmpty(), embedInteropTypes);
    }

    private async ValueTask SerializeMetadataReferenceAsync(MetadataReference reference, ObjectWriter writer, SolutionReplicationContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await WriteMetadataReferenceToAsync(reference, writer, context, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<MetadataReference> DeserializeMetadataReferenceAsync(ObjectReader reader, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await ReadMetadataReferenceFromAsync(reader, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask SerializeAnalyzerReferenceAsync(AnalyzerReference reference, ObjectWriter writer, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await WriteAnalyzerReferenceToAsync(reference, writer, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<AnalyzerReference> DeserializeAnalyzerReferenceAsync(ObjectReader reader, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await ReadAnalyzerReferenceFromAsync(reader, cancellationToken).ConfigureAwait(false);
    }
}
