// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Cryptography;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// A small struct that holds the values that define the identity of a source generated document, and don't change
/// as new generations happen. This is mostly for convenience as we are reguarly working with this combination of values.
/// </summary>
[DataContract]
internal readonly record struct SourceGeneratedDocumentIdentity : IEquatable<SourceGeneratedDocumentIdentity>
{
    [DataMember(Order = 0)] public DocumentId DocumentId { get; }
    [DataMember(Order = 1)] public string HintName { get; }
    [DataMember(Order = 2)] public SourceGeneratorIdentity Generator { get; }
    [DataMember(Order = 3)] public string FilePath { get; }

    public SourceGeneratedDocumentIdentity(DocumentId documentId, string hintName, SourceGeneratorIdentity generator, string filePath)
    {
        Contract.ThrowIfFalse(documentId.IsSourceGenerated);
        DocumentId = documentId;
        HintName = hintName;
        Generator = generator;
        FilePath = filePath;
    }

    public static SourceGeneratedDocumentIdentity Generate(ProjectId projectId, string hintName, ISourceGenerator generator, string filePath, AnalyzerReference analyzerReference)
    {
        // We want the DocumentId generated for a generated output to be stable between Compilations; this is so
        // features that track a document by DocumentId can find it after some change has happened that requires
        // generators to run again. To achieve this we'll just do a crytographic hash of the generator name and hint
        // name; the choice of a cryptographic hash as opposed to a more generic string hash is we actually want to
        // ensure we don't have collisions.
        var generatorIdentity = SourceGeneratorIdentity.Create(generator, analyzerReference);

        // The assembly path should exist in any normal scenario; the hashing of the name only would apply if the user loaded a
        // dynamic assembly they produced at runtime and passed us that via a custom AnalyzerReference.
        var assemblyNameToHash = generatorIdentity.AssemblyPath ?? generatorIdentity.AssemblyName;

#if NET
        Span<byte> bytesToChecksum = stackalloc byte[16];
        projectId.Id.TryWriteBytes(bytesToChecksum);
#else
        var bytesToChecksum = projectId.Id.ToByteArray().AsSpan();
#endif

        ReadOnlySpan<string> stringsToChecksum = [assemblyNameToHash, generatorIdentity.TypeName, hintName];
        var stringChecksum = Checksum.Create(stringsToChecksum);
        var byteChecksum = Checksum.Create(bytesToChecksum);
        var compositeChecksum = Checksum.Create(stringChecksum, byteChecksum);

        Span<byte> checksumAsBytes = stackalloc byte[16];
        compositeChecksum.WriteTo(checksumAsBytes);

#if NET
        var guid = new Guid(checksumAsBytes);
#else
        var guid = new Guid(checksumAsBytes.ToArray());
#endif

        var documentId = DocumentId.CreateFromSerialized(projectId, guid, isSourceGenerated: true, hintName);

        return new SourceGeneratedDocumentIdentity(documentId, hintName, generatorIdentity, filePath);
    }

    public void WriteTo(ObjectWriter writer)
    {
        DocumentId.WriteTo(writer);

        writer.WriteString(HintName);
        writer.WriteString(Generator.AssemblyName);
        writer.WriteString(Generator.AssemblyPath);
        writer.WriteString(Generator.AssemblyVersion.ToString());
        writer.WriteString(Generator.TypeName);
        writer.WriteString(FilePath);
    }

    internal static SourceGeneratedDocumentIdentity ReadFrom(ObjectReader reader)
    {
        var documentId = DocumentId.ReadFrom(reader);

        var hintName = reader.ReadRequiredString();
        var generatorAssemblyName = reader.ReadRequiredString();
        var generatorAssemblyPath = reader.ReadString();
        var generatorAssemblyVersion = Version.Parse(reader.ReadRequiredString());
        var generatorTypeName = reader.ReadRequiredString();
        var filePath = reader.ReadRequiredString();

        return new SourceGeneratedDocumentIdentity(
            documentId,
            hintName,
            new SourceGeneratorIdentity(
                generatorAssemblyName,
                generatorAssemblyPath,
                generatorAssemblyVersion,
                generatorTypeName),
            filePath);
    }
}
