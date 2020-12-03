// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.InternalUtilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Utilities;

namespace Roslyn.Test.Utilities.PDB
{
    internal class TestMetadataReferenceInfo : IDisposable
    {
        public readonly Compilation Compilation;
        public readonly TestMetadataReference MetadataReference;
        public readonly MetadataReferenceInfo MetadataReferenceInfo;
        private bool _disposedValue;
        private readonly MemoryStream _emitStream;
        private readonly PEReader _peReader;

        public TestMetadataReferenceInfo(
            MemoryStream emitStream,
            Compilation compilation,
            TestMetadataReference metadataReference,
            string fullPath)
        {
            _emitStream = emitStream;
            _peReader = new PEReader(emitStream);
            Compilation = compilation;
            MetadataReference = metadataReference;

            var metadataReader = _peReader.GetMetadataReader();
            var moduleDefinition = metadataReader.GetModuleDefinition();

            MetadataReferenceInfo = new MetadataReferenceInfo(
                _peReader.PEHeaders.CoffHeader.TimeDateStamp,
                _peReader.PEHeaders.PEHeader.SizeOfImage,
                PathUtilities.GetFileName(fullPath),
                metadataReader.GetGuid(moduleDefinition.Mvid),
                metadataReference.Properties.Aliases,
                metadataReference.Properties.Kind,
                metadataReference.Properties.EmbedInteropTypes);
        }

        public static TestMetadataReferenceInfo Create(Compilation compilation, string fullPath, EmitOptions emitOptions)
        {
            var emitStream = compilation.EmitToStream(emitOptions);

            var metadata = AssemblyMetadata.CreateFromStream(emitStream);
            var metadataReference = new TestMetadataReference(metadata, fullPath: fullPath);

            return new TestMetadataReferenceInfo(
                emitStream,
                compilation,
                metadataReference,
                fullPath);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _peReader.Dispose();
                    _emitStream.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
