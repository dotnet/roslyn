// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A small struct that holds the values that define the identity of a source generated document, and don't change
    /// as new generations happen. This is mostly for convenience as we are reguarly working with this combination of values.
    /// </summary>
    internal readonly struct SourceGeneratedDocumentIdentity : IObjectWritable, IEquatable<SourceGeneratedDocumentIdentity>
    {
        public DocumentId DocumentId { get; }
        public string HintName { get; }
        public string GeneratorAssemblyName { get; }
        public string GeneratorTypeName { get; }
        public string FilePath { get; }

        public bool ShouldReuseInSerialization => true;

        public SourceGeneratedDocumentIdentity(DocumentId documentId, string hintName, string generatorAssemblyName, string generatorTypeName, string filePath)
        {
            DocumentId = documentId;
            HintName = hintName;
            GeneratorAssemblyName = generatorAssemblyName;
            GeneratorTypeName = generatorTypeName;
            FilePath = filePath;
        }

        public static string GetGeneratorTypeName(ISourceGenerator generator)
        {
            return generator.GetType().FullName!;
        }

        public static string GetGeneratorAssemblyName(ISourceGenerator generator)
        {
            return generator.GetType().Assembly.FullName!;
        }

        public static SourceGeneratedDocumentIdentity Generate(ProjectId projectId, string hintName, ISourceGenerator generator, string filePath)
        {
            // We want the DocumentId generated for a generated output to be stable between Compilations; this is so features that track
            // a document by DocumentId can find it after some change has happened that requires generators to run again.
            // To achieve this we'll just do a crytographic hash of the generator name and hint name; the choice of a cryptographic hash
            // as opposed to a more generic string hash is we actually want to ensure we don't have collisions.
            var generatorAssemblyName = GetGeneratorAssemblyName(generator);
            var generatorTypeName = GetGeneratorTypeName(generator);

            // Combine the strings together; we'll use Encoding.Unicode since that'll match the underlying format; this can be made much
            // faster once we're on .NET Core since we could directly treat the strings as ReadOnlySpan<char>.
            var projectIdBytes = projectId.Id.ToByteArray();
            using var _ = ArrayBuilder<byte>.GetInstance(capacity: (generatorAssemblyName.Length + 1 + generatorTypeName.Length + 1 + hintName.Length) * 2 + projectIdBytes.Length, out var hashInput);
            hashInput.AddRange(projectIdBytes);

            // Add a null to separate the generator name and hint name; since this is effectively a joining of UTF-16 bytes
            // we'll use a UTF-16 null just to make sure there's absolutely no risk of collision.
            hashInput.AddRange(Encoding.Unicode.GetBytes(generatorAssemblyName));
            hashInput.AddRange(0, 0);
            hashInput.AddRange(Encoding.Unicode.GetBytes(generatorTypeName));
            hashInput.AddRange(0, 0);
            hashInput.AddRange(Encoding.Unicode.GetBytes(hintName));

            // The particular choice of crypto algorithm here is arbitrary and can be always changed as necessary. The only requirement
            // is it must be collision resistant, and provide enough bits to fill a GUID.
            using var crytpoAlgorithm = System.Security.Cryptography.SHA256.Create();
            var hash = crytpoAlgorithm.ComputeHash(hashInput.ToArray());
            Array.Resize(ref hash, 16);
            var guid = new Guid(hash);

            var documentId = DocumentId.CreateFromSerialized(projectId, guid, hintName);

            return new SourceGeneratedDocumentIdentity(documentId, hintName, generatorAssemblyName, generatorTypeName, filePath);
        }

        public void WriteTo(ObjectWriter writer)
        {
            DocumentId.WriteTo(writer);

            writer.WriteString(HintName);
            writer.WriteString(GeneratorAssemblyName);
            writer.WriteString(GeneratorTypeName);
            writer.WriteString(FilePath);
        }

        internal static SourceGeneratedDocumentIdentity ReadFrom(ObjectReader reader)
        {
            var documentId = DocumentId.ReadFrom(reader);

            var hintName = reader.ReadString();
            var generatorAssemblyName = reader.ReadString();
            var generatorTypeName = reader.ReadString();
            var filePath = reader.ReadString();

            return new SourceGeneratedDocumentIdentity(documentId, hintName, generatorAssemblyName, generatorTypeName, filePath);
        }

        public override bool Equals(object? obj)
        {
            return obj is SourceGeneratedDocumentIdentity identity && Equals(identity);
        }

        public bool Equals(SourceGeneratedDocumentIdentity other)
        {
            return EqualityComparer<DocumentId>.Default.Equals(DocumentId, other.DocumentId) &&
                   HintName == other.HintName &&
                   GeneratorAssemblyName == other.GeneratorAssemblyName &&
                   GeneratorTypeName == other.GeneratorTypeName &&
                   FilePath == other.FilePath;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(DocumentId,
                   Hash.Combine(HintName,
                   Hash.Combine(GeneratorAssemblyName,
                   Hash.Combine(GeneratorTypeName,
                   Hash.Combine(FilePath, 0)))));
        }

        public static bool operator ==(SourceGeneratedDocumentIdentity left, SourceGeneratedDocumentIdentity right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SourceGeneratedDocumentIdentity left, SourceGeneratedDocumentIdentity right)
        {
            return !(left == right);
        }
    }
}
