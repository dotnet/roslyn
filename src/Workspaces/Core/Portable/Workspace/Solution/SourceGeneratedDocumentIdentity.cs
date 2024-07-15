// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
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

        // Combine the strings together; we'll use Encoding.Unicode since that'll match the underlying format; this can be made much
        // faster once we're on .NET Core since we could directly treat the strings as ReadOnlySpan<char>.
        var projectIdBytes = projectId.Id.ToByteArray();

        // The assembly path should exist in any normal scenario; the hashing of the name only would apply if the user loaded a
        // dynamic assembly they produced at runtime and passed us that via a custom AnalyzerReference.
        var assemblyNameToHash = generatorIdentity.AssemblyPath ?? generatorIdentity.AssemblyName;

        using var _ = ArrayBuilder<byte>.GetInstance(capacity: (assemblyNameToHash.Length + 1 + generatorIdentity.TypeName.Length + 1 + hintName.Length) * 2 + projectIdBytes.Length, out var hashInput);
        hashInput.AddRange(projectIdBytes);

        // Add a null to separate the generator name and hint name; since this is effectively a joining of UTF-16 bytes
        // we'll use a UTF-16 null just to make sure there's absolutely no risk of collision.
        hashInput.AddRange(Encoding.Unicode.GetBytes(assemblyNameToHash));
        hashInput.AddRange(0, 0);
        hashInput.AddRange(Encoding.Unicode.GetBytes(generatorIdentity.TypeName));
        hashInput.AddRange(0, 0);
        hashInput.AddRange(Encoding.Unicode.GetBytes(hintName));

        // The particular choice of crypto algorithm here is arbitrary and can be always changed as necessary. The only requirement
        // is it must be collision resistant, and provide enough bits to fill a GUID.
        using var crytpoAlgorithm = System.Security.Cryptography.SHA256.Create();
        var hash = crytpoAlgorithm.ComputeHash(hashInput.ToArray());
        Array.Resize(ref hash, 16);
        var guid = new Guid(hash);

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
