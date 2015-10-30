// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis.InternalUtilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents metadata image reference.
    /// </summary>
    /// <remarks>
    /// Represents a logical location of the image, not the content of the image. 
    /// The content might change in time. A snapshot is taken when the compiler queries the reference for its metadata.
    /// </remarks>
    public abstract class MetadataReference
    {
        public MetadataReferenceProperties Properties { get; }

        protected MetadataReference(MetadataReferenceProperties properties)
        {
            this.Properties = properties;
        }

        /// <summary>
        /// Path or name used in error messages to identity the reference.
        /// </summary>
        public virtual string Display { get { return null; } }

        /// <summary>
        /// Returns true if this reference is an unresolved reference.
        /// </summary>
        internal virtual bool IsUnresolved
        {
            get { return false; }
        }

        /// <summary>
        /// Returns an instance of the reference with specified aliases.
        /// </summary>
        /// <param name="aliases">The new aliases for the reference.</param>
        /// <exception cref="ArgumentException">Alias is invalid for the metadata kind.</exception> 
        public MetadataReference WithAliases(IEnumerable<string> aliases)
        {
            return WithAliases(ImmutableArray.CreateRange(aliases));
        }

        /// <summary>
        /// Returns an instance of the reference with specified interop types embedding.
        /// </summary>
        /// <param name="value">The new value for <see cref="MetadataReferenceProperties.EmbedInteropTypes"/>.</param>
        /// <exception cref="ArgumentException">Interop types can't be embedded from modules.</exception> 
        public MetadataReference WithEmbedInteropTypes(bool value)
        {
            return WithProperties(Properties.WithEmbedInteropTypes(value));
        }

        /// <summary>
        /// Returns an instance of the reference with specified aliases.
        /// </summary>
        /// <param name="aliases">The new aliases for the reference.</param>
        /// <exception cref="ArgumentException">Alias is invalid for the metadata kind.</exception> 
        public MetadataReference WithAliases(ImmutableArray<string> aliases)
        {
            return WithProperties(Properties.WithAliases(aliases));
        }

        /// <summary>
        /// Returns an instance of the reference with specified properties, or this instance if properties haven't changed.
        /// </summary>
        /// <param name="properties">The new properties for the reference.</param>
        /// <exception cref="ArgumentException">Specified values not valid for this reference.</exception>
        public MetadataReference WithProperties(MetadataReferenceProperties properties)
        {
            if (properties == this.Properties)
            {
                return this;
            }

            return WithPropertiesImplReturningMetadataReference(properties);
        }

        internal abstract MetadataReference WithPropertiesImplReturningMetadataReference(MetadataReferenceProperties properties);

        /// <summary>
        /// Creates a reference to a single-module assembly or a standalone module stored in memory.
        /// </summary>
        /// <param name="peImage">Assembly image.</param>
        /// <param name="properties">Reference properties (extern aliases, type embedding, <see cref="MetadataImageKind"/>).</param>
        /// <param name="documentation">Provides XML documentation for symbol found in the reference.</param>
        /// <param name="filePath">Optional path that describes the location of the metadata. The file doesn't need to exist on disk. The path is opaque to the compiler.</param>
        /// <remarks>
        /// Performance considerations: 
        /// <para>
        /// It is recommended to use <see cref="AssemblyMetadata.CreateFromImage(ImmutableArray{byte})"/> or <see cref="ModuleMetadata.CreateFromImage(ImmutableArray{byte})"/> 
        /// API when creating multiple references to the same metadata.
        /// Reusing <see cref="Metadata"/> object to create multiple references allows for sharing data across these references.
        /// </para> 
        /// <para>
        /// The method pins <paramref name="peImage"/> in managed heap. The pinned memory is released 
        /// when the resulting reference becomes unreachable and GC collects it. To control the lifetime of the pinned memory 
        /// deterministically use <see cref="AssemblyMetadata.CreateFromImage(ImmutableArray{byte})"/> 
        /// to create an <see cref="IDisposable"/> metadata object and 
        /// <see cref="AssemblyMetadata.GetReference(DocumentationProvider, ImmutableArray{string}, bool, string, string)"/> to get a reference to it.
        /// </para>
        /// <para>
        /// The method creates a reference to a single-module assembly. To create a reference to a multi-module assembly or a stand-alone module use 
        /// <see cref="ModuleMetadata.CreateFromImage(ImmutableArray{byte})"/> and <see cref="ModuleMetadata.GetReference(DocumentationProvider, string, string)"/>.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="peImage"/> is null.</exception>
        public static PortableExecutableReference CreateFromImage(
            ImmutableArray<byte> peImage,
            MetadataReferenceProperties properties = default(MetadataReferenceProperties),
            DocumentationProvider documentation = null,
            string filePath = null)
        {
            var metadata = AssemblyMetadata.CreateFromImage(peImage);
            return new MetadataImageReference(metadata, properties, documentation, filePath, display: null);
        }

        /// <summary>
        /// Creates a reference to a single-module assembly or a standalone module stored in memory.
        /// </summary>
        /// <param name="peImage">Assembly image.</param>
        /// <param name="properties">Reference properties (extern aliases, type embedding, <see cref="MetadataImageKind"/>).</param>
        /// <param name="documentation">Provides XML documentation for symbol found in the reference.</param>
        /// <param name="filePath">Optional path that describes the location of the metadata. The file doesn't need to exist on disk. The path is opaque to the compiler.</param>
        /// <remarks>
        /// Performance considerations: 
        /// <para>
        /// It is recommended to use <see cref="AssemblyMetadata.CreateFromImage(IEnumerable{byte})"/> or <see cref="ModuleMetadata.CreateFromImage(IEnumerable{byte})"/> 
        /// API when creating multiple references to the same metadata.
        /// Reusing <see cref="Metadata"/> object to create multiple references allows for sharing data across these references.
        /// </para> 
        /// <para>
        /// The method makes a copy of the data and pins it. To avoid making a copy use an overload that takes an <see cref="ImmutableArray{T}"/>.
        /// The pinned memory is released when the resulting reference becomes unreachable and GC collects it. To control the lifetime of the pinned memory 
        /// deterministically use <see cref="AssemblyMetadata.CreateFromStream(Stream, PEStreamOptions)"/> 
        /// to create an <see cref="IDisposable"/> metadata object and 
        /// <see cref="AssemblyMetadata.GetReference(DocumentationProvider, ImmutableArray{string}, bool, string, string)"/> to get a reference to it.
        /// to get a reference to it.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="peImage"/> is null.</exception>
        public static PortableExecutableReference CreateFromImage(
            IEnumerable<byte> peImage,
            MetadataReferenceProperties properties = default(MetadataReferenceProperties),
            DocumentationProvider documentation = null,
            string filePath = null)
        {
            var metadata = AssemblyMetadata.CreateFromImage(peImage);
            return new MetadataImageReference(metadata, properties, documentation, filePath, display: null);
        }

        /// <summary>
        /// Creates a reference to a single-module assembly or a stand-alone module from data in specified stream. 
        /// Reads the content of the stream into memory and closes the stream upon return.
        /// </summary>
        /// <param name="peStream">Assembly image.</param>
        /// <param name="properties">Reference properties (extern aliases, type embedding, <see cref="MetadataImageKind"/>).</param>
        /// <param name="documentation">Provides XML documentation for symbol found in the reference.</param>
        /// <param name="filePath">Optional path that describes the location of the metadata. The file doesn't need to exist on disk. The path is opaque to the compiler.</param>
        /// <exception cref="ArgumentException"><paramref name="peStream"/> doesn't support read and seek operations.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="peStream"/> is null.</exception>
        /// <exception cref="IOException">An error occurred while reading the stream.</exception>
        /// <remarks>
        /// Performance considerations: 
        /// <para>
        /// It is recommended to use <see cref="AssemblyMetadata.CreateFromStream(Stream, PEStreamOptions)"/> or <see cref="ModuleMetadata.CreateFromStream(Stream, PEStreamOptions)"/> 
        /// API when creating multiple references to the same metadata.
        /// Reusing <see cref="Metadata"/> object to create multiple references allows for sharing data across these references.
        /// </para> 
        /// <para>
        /// The method eagerly reads the entire content of <paramref name="peStream"/> into native heap. The native memory block is released 
        /// when the resulting reference becomes unreachable and GC collects it. To decrease memory footprint of the reference and/or manage
        /// the lifetime deterministically use <see cref="AssemblyMetadata.CreateFromStream(Stream, PEStreamOptions)"/> 
        /// to create an <see cref="IDisposable"/> metadata object and 
        /// <see cref="AssemblyMetadata.GetReference(DocumentationProvider, ImmutableArray{string}, bool, string, string)"/> to get a reference to it.
        /// to get a reference to it.
        /// </para>
        /// </remarks>
        public static PortableExecutableReference CreateFromStream(
            Stream peStream,
            MetadataReferenceProperties properties = default(MetadataReferenceProperties),
            DocumentationProvider documentation = null,
            string filePath = null)
        {
            // Prefetch data and close the stream. 
            var metadata = AssemblyMetadata.CreateFromStream(peStream, PEStreamOptions.PrefetchEntireImage);

            return new MetadataImageReference(metadata, properties, documentation, filePath, display: null);
        }

        /// <summary>
        /// Creates a reference to an assembly or standalone module stored in a file.
        /// Reads the content of the file into memory.
        /// </summary>
        /// <param name="path">Path to the assembly file.</param>
        /// <param name="properties">Reference properties (extern aliases, type embedding, <see cref="MetadataImageKind"/>).</param>
        /// <param name="documentation">Provides XML documentation for symbol found in the reference.</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is invalid.</exception>
        /// <exception cref="IOException">An error occurred while reading the file.</exception>
        /// <remarks>
        /// Performance considerations:
        /// <para>
        /// It is recommended to use <see cref="AssemblyMetadata.CreateFromFile(string)"/> or <see cref="ModuleMetadata.CreateFromFile(string)"/> 
        /// API when creating multiple references to the same file.
        /// Reusing <see cref="Metadata"/> object allows for sharing data across these references.
        /// </para> 
        /// <para>
        /// The method eagerly reads the entire content of the file into native heap. The native memory block is released 
        /// when the resulting reference becomes unreachable and GC collects it. To decrease memory footprint of the reference and/or manage
        /// the lifetime deterministically use <see cref="AssemblyMetadata.CreateFromFile(string)"/> 
        /// to create an <see cref="IDisposable"/> metadata object and 
        /// <see cref="AssemblyMetadata.GetReference(DocumentationProvider, ImmutableArray{string}, bool, string, string)"/> 
        /// to get a reference to it.
        /// </para>
        /// </remarks>
        public static PortableExecutableReference CreateFromFile(
            string path,
            MetadataReferenceProperties properties = default(MetadataReferenceProperties),
            DocumentationProvider documentation = null)
        {
            var peStream = FileUtilities.OpenFileStream(path);

            // prefetch image, close stream to avoid locking it:
            var module = ModuleMetadata.CreateFromStream(peStream, PEStreamOptions.PrefetchEntireImage);

            if (properties.Kind == MetadataImageKind.Module)
            {
                return new MetadataImageReference(module, properties, documentation, path, display: null);
            }

            // any additional modules constituting the assembly will be read lazily:
            var assemblyMetadata = AssemblyMetadata.CreateFromFile(module, path);
            return new MetadataImageReference(assemblyMetadata, properties, documentation, path, display: null);
        }

        /// <summary>
        /// Creates a reference to a loaded assembly.
        /// </summary>
        /// <param name="assembly">Path to the module file.</param>
        /// <exception cref="ArgumentNullException"><paramref name="assembly"/> is null.</exception>
        /// <exception cref="NotSupportedException"><paramref name="assembly"/> is dynamic, doesn't have a location, or the platform doesn't support reading from the location.</exception>
        /// <remarks>
        /// Performance considerations:
        /// <para>
        /// It is recommended to use <see cref="AssemblyMetadata.CreateFromFile(string)"/> API when creating multiple references to the same assembly.
        /// Reusing <see cref="AssemblyMetadata"/> object allows for sharing data across these references.
        /// </para>
        /// </remarks>
        [Obsolete("Use CreateFromFile(assembly.Location) instead", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MetadataReference CreateFromAssembly(Assembly assembly)
        {
            return CreateFromAssemblyInternal(assembly);
        }

        internal static MetadataReference CreateFromAssemblyInternal(Assembly assembly)
        {
            return CreateFromAssemblyInternal(assembly, default(MetadataReferenceProperties));
        }

        /// <summary>
        /// Creates a reference to a loaded assembly.
        /// </summary>
        /// <param name="assembly">Path to the module file.</param>
        /// <param name="properties">Reference properties (extern aliases, type embedding).</param>
        /// <param name="documentation">Provides XML documentation for symbol found in the reference.</param>
        /// <exception cref="ArgumentNullException"><paramref name="assembly"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="properties"/>.<see cref="MetadataReferenceProperties.Kind"/> is not <see cref="MetadataImageKind.Assembly"/>.</exception>
        /// <exception cref="NotSupportedException"><paramref name="assembly"/> is dynamic, doesn't have a location, or the platform doesn't support reading from the location.</exception>
        /// <remarks>
        /// Performance considerations:
        /// <para>
        /// It is recommended to use <see cref="AssemblyMetadata.CreateFromFile(string)"/> API when creating multiple references to the same assembly.
        /// Reusing <see cref="AssemblyMetadata"/> object allows for sharing data across these references.
        /// </para>
        /// </remarks>
        [Obsolete("Use CreateFromFile(assembly.Location) instead", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static MetadataReference CreateFromAssembly(
            Assembly assembly,
            MetadataReferenceProperties properties,
            DocumentationProvider documentation = null)
        {
            return CreateFromAssemblyInternal(assembly, properties, documentation);
        }

        internal static MetadataReference CreateFromAssemblyInternal(
            Assembly assembly,
            MetadataReferenceProperties properties,
            DocumentationProvider documentation = null)
        {
            // Note: returns MetadataReference and not PortableExecutableReference so that we can in future support assemblies that
            // which are not backed by PE image.

            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            if (assembly.IsDynamic)
            {
                throw new NotSupportedException(CodeAnalysisResources.CantCreateReferenceToDynamicAssembly);
            }

            if (properties.Kind != MetadataImageKind.Assembly)
            {
                throw new ArgumentException(CodeAnalysisResources.CantCreateModuleReferenceToAssembly, nameof(properties));
            }

            string location = CorLightup.Desktop.GetAssemblyLocation(assembly);
            if (string.IsNullOrEmpty(location))
            {
                throw new NotSupportedException(CodeAnalysisResources.CantCreateReferenceToAssemblyWithoutLocation);
            }

            Stream peStream = FileUtilities.OpenFileStream(location);

            // The file is locked by the CLR assembly loader, so we can create a lazily read metadata, 
            // which might also lock the file until the reference is GC'd.
            var metadata = AssemblyMetadata.CreateFromStream(peStream);

            return new MetadataImageReference(metadata, properties, documentation, location, display: null);
        }

        internal static bool HasMetadata(Assembly assembly)
        {
            return !assembly.IsDynamic && !string.IsNullOrEmpty(CorLightup.Desktop.GetAssemblyLocation(assembly));
        }
    }
}
