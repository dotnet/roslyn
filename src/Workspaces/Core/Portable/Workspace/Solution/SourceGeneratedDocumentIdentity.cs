// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A small struct that holds the values that define the identity of a source generated document, and don't change
    /// as new generations happen. This is mostly for convenience as we are reguarly working with this combination of values.
    /// </summary>
    internal readonly struct SourceGeneratedDocumentIdentity
    {
        public DocumentId DocumentId { get; }
        public string HintName { get; }
        public ISourceGenerator Generator { get; }
        public string FilePath { get; }

        public SourceGeneratedDocumentIdentity(DocumentId documentId, string hintName, ISourceGenerator generator, string filePath)
        {
            DocumentId = documentId;
            HintName = hintName;
            Generator = generator;
            FilePath = filePath;
        }

        public static SourceGeneratedDocumentIdentity Generate(ProjectId projectId, string hintName, ISourceGenerator generator, string filePath)
        {
            // We want the DocumentId generated for a generated output to be stable between Compilations; this is so features that track
            // a document by DocumentId can find it after some change has happened that requires generators to run again.
            // To achieve this we'll just do a crytographic hash of the generator name and hint name; the choice of a cryptographic hash
            // as opposed to a more generic string hash is we actually want to ensure we don't have collisions.
            var generatorName = generator.GetType().FullName;
            Contract.ThrowIfNull(generatorName);

            // Combine the strings together; we'll use Encoding.Unicode since that'll match the underlying format; this can be made much
            // faster once we're on .NET Core since we could directly treat the strings as ReadOnlySpan<char>.
            var projectIdBytes = projectId.Id.ToByteArray();
            using var _ = ArrayBuilder<byte>.GetInstance(capacity: (generatorName.Length + hintName.Length + 1) * 2 + projectIdBytes.Length, out var hashInput);
            hashInput.AddRange(projectIdBytes);
            hashInput.AddRange(Encoding.Unicode.GetBytes(generatorName));

            // Add a null to separate the generator name and hint name; since this is effectively a joining of UTF-16 bytes
            // we'll use a UTF-16 null just to make sure there's absolutely no risk of collision.
            hashInput.AddRange(0, 0);
            hashInput.AddRange(Encoding.Unicode.GetBytes(hintName));

            // The particular choice of crypto algorithm here is arbitrary and can be always changed as necessary. The only requirement
            // is it must be collision resistant, and provide enough bits to fill a GUID.
            using var crytpoAlgorithm = System.Security.Cryptography.SHA256.Create();
            var hash = crytpoAlgorithm.ComputeHash(hashInput.ToArray());
            Array.Resize(ref hash, 16);
            var guid = new Guid(hash);

            var documentId = DocumentId.CreateFromSerialized(projectId, guid, hintName);

            return new SourceGeneratedDocumentIdentity(documentId, hintName, generator, filePath);
        }
    }
}
