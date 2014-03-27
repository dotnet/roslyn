// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Roslyn.Utilities;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents an in-memory Portable-Executable image.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    public sealed class MetadataImageReference : PortableExecutableReference
    {
        private readonly string display;
        private readonly Metadata metadata;

        /// <summary>
        /// Creates a reference to a single-module assembly image.
        /// </summary>
        /// <param name="assemblyImage">Read-only assembly image.</param>
        /// <param name="documentation">Provides XML documentation for symbol found in the reference.</param>
        /// <param name="aliases">Reference aliases.</param>
        /// <param name="embedInteropTypes">True if interop types contained in the reference should be embedded to the compilation that uses the reference.</param>
        /// <param name="fullPath">Optional full path used for reference comparison when used in compilation. The file doesn't need to exist.</param>
        /// <param name="display">Display string for error reporting.</param>
        public MetadataImageReference(ImmutableArray<byte> assemblyImage, DocumentationProvider documentation = null, ImmutableArray<string> aliases = default(ImmutableArray<string>), bool embedInteropTypes = false, string fullPath = null, string display = null)
            : this(AssemblyMetadata.CreateFromImage(RequireNonNull(assemblyImage, "assemblyImage")), documentation, aliases, embedInteropTypes, fullPath, display)
        {
        }

        /// <summary>
        /// Creates a reference to a single-module assembly image.
        /// </summary>
        /// <param name="assemblyImage">Read-only assembly image.</param>
        /// <param name="documentation">Provides XML documentation for symbol found in the reference.</param>
        /// <param name="aliases">Reference aliases.</param>
        /// <param name="embedInteropTypes">True if interop types contained in the reference should be embedded to the compilation that uses the reference.</param>
        /// <param name="fullPath">Optional full path used for reference comparison when used in compilation. The file doesn't need to exist.</param>
        /// <param name="display">Display string for error reporting.</param>
        public MetadataImageReference(IEnumerable<byte> assemblyImage, DocumentationProvider documentation = null, ImmutableArray<string> aliases = default(ImmutableArray<string>), bool embedInteropTypes = false, string fullPath = null, string display = null)
            : this(assemblyImage.AsImmutableOrNull(), documentation, aliases, embedInteropTypes, fullPath, display)
        {
        }

        /// <summary>
        /// Creates a reference to a single-module assembly image.
        /// </summary>
        /// <param name="assemblyImage">Stream with assembly image, it should support seek operations.</param>
        /// <param name="documentation">Provides XML documentation for symbol found in the reference.</param>
        /// <param name="aliases">Reference alias.</param>
        /// <param name="embedInteropTypes">True if interop types contained in the reference should be embedded to the compilation that uses the reference.</param>
        /// <param name="fullPath">Optional full path used for reference comparison when used in compilation. The file doesn't need to exist.</param>
        /// <param name="display">Display string for error reporting.</param>
        public MetadataImageReference(System.IO.Stream assemblyImage, DocumentationProvider documentation = null, ImmutableArray<string> aliases = default(ImmutableArray<string>), bool embedInteropTypes = false, string fullPath = null, string display = null)
            : this(AssemblyMetadata.CreateFromImageStream(RequireNonNull(assemblyImage, "assemblyImage")), documentation, aliases, embedInteropTypes, fullPath, display)
        {
        }

        /// <summary>
        /// Creates a reference to a standalone module image.
        /// </summary>
        /// <param name="metadata">Metadata for the standalone module.</param>
        /// <param name="fullPath">
        /// Optional full path used for reference comparison when used in compilation. 
        /// The file doesn't need to exist.
        /// If <paramref name="metadata"/> represents a memory mapped file and this parameter is not specified the path to the memory mapped file is used.
        /// </param>
        /// <param name="documentation">Provides XML documentation for symbol found in the reference.</param>
        /// <param name="display">Display string for error reporting.</param>
        public MetadataImageReference(ModuleMetadata metadata, DocumentationProvider documentation = null, string fullPath = null, string display = null)
            : this(RequireNonNull(metadata, "metadata"), documentation, MetadataReferenceProperties.Module, fullPath, display)
        {
        }

        /// <summary>
        /// Creates a reference to an assembly or a module image. The assembly can comprise multiple modules.
        /// </summary>
        /// <param name="metadata">Assembly or module metadata.</param>
        /// <param name="documentation">Provides XML documentation for symbol found in the reference.</param>
        /// <param name="aliases">Reference aliases.</param>
        /// <param name="embedInteropTypes">True if interop types contained in the reference should be embedded to the compilation that uses the reference.</param>
        /// <param name="fullPath">
        /// Optional full path used for reference comparison when used in compilation. 
        /// The file doesn't need to exist.
        /// If the manifest module of the assembly is a memory mapped file and this parameter is not specified the path to the memory mapped file is used.
        /// </param>
        /// <param name="display">Display string for error reporting.</param>
        public MetadataImageReference(
            AssemblyMetadata metadata,
            DocumentationProvider documentation = null,
            ImmutableArray<string> aliases = default(ImmutableArray<string>),
            bool embedInteropTypes = false,
            string fullPath = null,
            string display = null)
            : this(RequireNonNull(metadata, "metadata"),
                   documentation,
                   new MetadataReferenceProperties(metadata.Kind, aliases, embedInteropTypes),
                   fullPath,
                   display)
        {
        }

        private MetadataImageReference(Metadata metadata, DocumentationProvider documentation, MetadataReferenceProperties properties, string fullPath, string display)
            : base(properties, fullPath, documentation ?? DocumentationProvider.Default)
        {
            this.display = display;
            this.metadata = metadata;
        }

        protected override Metadata GetMetadataImpl()
        {
            return metadata;
        }

        protected override DocumentationProvider CreateDocumentationProvider()
        {
            // documentation provider is initialized in the constructor
            throw ExceptionUtilities.Unreachable;
        }

        /// <summary>
        /// Returns an instance of the reference with specified aliases.
        /// </summary>
        /// <param name="aliases">The new aliases for the reference.</param>
        /// <exception cref="ArgumentException">Alias is invalid for the metadata kind.</exception> 
        public MetadataImageReference WithAliases(IEnumerable<string> aliases)
        {
            return WithAliases(ImmutableArray.CreateRange(aliases));
        }

        /// <summary>
        /// Returns an instance of the reference with specified aliases.
        /// </summary>
        /// <param name="aliases">The new aliases for the reference.</param>
        /// <exception cref="ArgumentException">Alias is invalid for the metadata kind.</exception> 
        public MetadataImageReference WithAliases(ImmutableArray<string> aliases)
        {
            if (aliases == this.Properties.Aliases)
            {
                return this;
            }

            return new MetadataImageReference(
                this.metadata,
                this.DocumentationProvider,
                new MetadataReferenceProperties(this.Properties.Kind, aliases, this.Properties.EmbedInteropTypes),
                this.FullPath,
                this.display);
        }

        /// <summary>
        /// Returns an instance of the reference with specified interop types embedding.
        /// </summary>
        /// <param name="value">The new value for <see cref="MetadataReferenceProperties.EmbedInteropTypes"/>.</param>
        /// <exception cref="ArgumentException">Interop types can't be embedded from modules.</exception> 
        public MetadataImageReference WithEmbedInteropTypes(bool value)
        {
            if (value == this.Properties.EmbedInteropTypes)
            {
                return this;
            }

            return new MetadataImageReference(
                this.metadata,
                this.DocumentationProvider,
                new MetadataReferenceProperties(this.Properties.Kind, this.Properties.Aliases, value),
                this.FullPath,
                this.display);
        }

        /// <summary>
        /// Returns an instance of the reference with specified documentation provider.
        /// </summary>
        /// <param name="provider">The new <see cref="DocumentationProvider"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="provider"/> is null.</exception> 
        public MetadataImageReference WithDocumentationProvider(DocumentationProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException("documentation");
            }

            if (ReferenceEquals(provider, this.DocumentationProvider))
            {
                return this;
            }

            return new MetadataImageReference(
                this.metadata,
                provider,
                this.Properties,
                this.FullPath,
                this.display);
        }

        private static ImmutableArray<byte> RequireNonNull(ImmutableArray<byte> arg, string name)
        {
            if (arg.IsDefault)
            {
                throw new ArgumentNullException(name);
            }

            return arg;
        }

        private static T RequireNonNull<T>(T arg, string name)
        {
            if (arg == null)
            {
                throw new ArgumentNullException(name);
            }

            return arg;
        }

        public override string Display
        {
            get
            {
                return display ?? FullPath ?? (Properties.Kind == MetadataImageKind.Assembly ? CodeAnalysisResources.InMemoryAssembly : CodeAnalysisResources.InMemoryModule);
            }
        }

        private string GetDebuggerDisplay()
        {
            var sb = new StringBuilder();
            sb.Append(Properties.Kind == MetadataImageKind.Module ? "Module" : "Assembly");
            if (!Properties.Aliases.IsDefaultOrEmpty)
            {
                sb.Append(" Aliases={");
                sb.Append(string.Join(", ", Properties.Aliases));
                sb.Append("}");
            }

            if (Properties.EmbedInteropTypes)
            {
                sb.Append(" Embed");
            }

            if (FullPath != null)
            {
                sb.Append(" Path='");
                sb.Append(FullPath);
                sb.Append("'");
            }

            if (display != null)
            {
                sb.Append(" Display='");
                sb.Append(display);
                sb.Append("'");
            }

            return sb.ToString();
        }
    }
}
