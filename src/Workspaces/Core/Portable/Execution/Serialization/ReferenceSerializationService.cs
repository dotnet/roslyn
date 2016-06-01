// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution.Serialization
{
    /// <summary>
    /// this is default implementation of IReferenceSerializationService
    /// </summary>
    [ExportWorkspaceService(typeof(IReferenceSerializationService), layer: ServiceLayer.Default), Shared]
    internal class ReferenceSerializationService : IReferenceSerializationService
    {
        private readonly static IAnalyzerAssemblyLoader s_loader = new AssemblyLoader();

        public void WriteTo(MetadataReference reference, ObjectWriter writer, CancellationToken cancellationToken)
        {
            // default implementation has no shadow copying. it also doesnt guarantee snapshot
            WriteTo(reference.Properties, writer, cancellationToken);

            var portable = reference as PortableExecutableReference;
            if (portable != null)
            {
                writer.WriteString(nameof(PortableExecutableReference));
                writer.WriteString(ReferenceSerializationKinds.FilePath);
                writer.WriteString(portable.FilePath);

                // TODO: what I should do with documentation provider? it is not exposed outside
                return;
            }

            throw ExceptionUtilities.UnexpectedValue(reference.GetType());
        }

        public void WriteTo(AnalyzerReference reference, ObjectWriter writer, CancellationToken cancellationToken)
        {
            writer.WriteString(reference.FullPath);

            var file = reference as AnalyzerFileReference;
            if (file != null)
            {
                writer.WriteString(nameof(AnalyzerFileReference));
                writer.WriteString(ReferenceSerializationKinds.FilePath);
                return;
            }

            var image = reference as AnalyzerImageReference;
            if (image != null)
            {
                // TODO: think a way to support this or a way to deal with this kind of situation.
                throw new NotSupportedException(nameof(AnalyzerImageReference));
            }

            var unresolved = reference as UnresolvedAnalyzerReference;
            if (unresolved != null)
            {
                writer.WriteString(nameof(UnresolvedAnalyzerReference));
                return;
            }

            throw ExceptionUtilities.UnexpectedValue(reference.GetType());
        }

        public MetadataReference ReadMetadataReferenceFrom(ObjectReader reader, CancellationToken cancellationToken)
        {
            var properties = ReadMetadataReferencePropertiesFrom(reader, cancellationToken);

            var type = reader.ReadString();
            if (type == nameof(PortableExecutableReference))
            {
                var kind = reader.ReadString();
                Contract.ThrowIfFalse(kind == ReferenceSerializationKinds.FilePath);

                // TODO: find a way to deal with documentation
                var filePath = reader.ReadString();
                return MetadataReference.CreateFromFile(filePath, properties, documentation: null);
            }

            throw ExceptionUtilities.UnexpectedValue(type);
        }

        public AnalyzerReference ReadAnalyzerReferenceFrom(ObjectReader reader, CancellationToken cancellationToken)
        {
            var fullPath = reader.ReadString();

            var type = reader.ReadString();
            if (type == nameof(AnalyzerFileReference))
            {
                var kind = reader.ReadString();
                Contract.ThrowIfFalse(kind == ReferenceSerializationKinds.FilePath);

                return new AnalyzerFileReference(fullPath, s_loader);
            }

            if (type == nameof(UnresolvedAnalyzerReference))
            {
                return new UnresolvedAnalyzerReference(fullPath);
            }

            throw ExceptionUtilities.UnexpectedValue(type);
        }

        private void WriteTo(MetadataReferenceProperties properties, ObjectWriter writer, CancellationToken cancellationToken)
        {
            writer.WriteInt32((int)properties.Kind);
            writer.WriteArray(properties.Aliases.ToArray());
            writer.WriteBoolean(properties.EmbedInteropTypes);
        }

        private MetadataReferenceProperties ReadMetadataReferencePropertiesFrom(ObjectReader reader, CancellationToken cancellationToken)
        {
            var kind = (MetadataImageKind)reader.ReadInt32();
            var aliases = reader.ReadArray<string>().ToImmutableArrayOrEmpty();
            var embedInteropTypes = reader.ReadBoolean();

            return new MetadataReferenceProperties(kind, aliases, embedInteropTypes);
        }

        private class AssemblyLoader : IAnalyzerAssemblyLoader
        {
            public void AddDependencyLocation(string fullPath)
            {
            }

            public Assembly LoadFromPath(string fullPath)
            {
                return Assembly.Load(new AssemblyName(fullPath));
            }
        }
    }
}
