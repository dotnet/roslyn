// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using Roslyn.Utilities;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents an immutable snapshot of assembly CLI metadata.
    /// </summary>
    public sealed class AssemblyMetadata : Metadata
    {
        /// <summary>
        /// Modules comprising this assembly. The first module is the manifest module.
        /// </summary>
        public ImmutableArray<ModuleMetadata> Modules { get; private set; }

        internal PEAssembly Assembly { get; private set; }

        /// <summary>
        /// Cached assembly symbols.
        /// </summary>
        /// <remarks>
        /// Guarded by <see cref="F:CommonReferenceManager.SymbolCacheAndReferenceManagerStateGuard"/>.
        /// </remarks>
        internal readonly WeakList<IAssemblySymbol> CachedSymbols = new WeakList<IAssemblySymbol>();

        private AssemblyMetadata(AssemblyMetadata metadata)
        {
            this.Assembly = metadata.Assembly;
            this.CachedSymbols = metadata.CachedSymbols;
            this.Modules = metadata.Modules.SelectAsArray(module => module.Copy());
        }

        private AssemblyMetadata(ModuleMetadata module)
        {
            this.Modules = ImmutableArray.Create(module);
            this.Assembly = new PEAssembly(this, ImmutableArray.Create(module.Module));
        }

        internal AssemblyMetadata(ImmutableArray<ModuleMetadata> modules)
        {
            this.Modules = modules;
            this.Assembly = new PEAssembly(this, modules.SelectAsArray(m => m.Module));
        }

        /// <summary>
        /// Creates a single-module assembly.
        /// </summary>
        /// <param name="peImage">
        /// Manifest module image.
        /// </param>
        /// <exception cref="ArgumentException"><paramref name="peImage"/> has the default value.</exception>
        /// <exception cref="BadImageFormatException">The PE image format is invalid.</exception>
        public static AssemblyMetadata CreateFromImage(ImmutableArray<byte> peImage)
        {
            return Create(ModuleMetadata.CreateFromImage(peImage));
        }

        /// <summary>
        /// Creates a single-module assembly.
        /// </summary>
        /// <param name="peImage">
        /// Manifest module image.
        /// </param>
        /// <exception cref="BadImageFormatException">The PE image format is invalid.</exception>
        public static AssemblyMetadata CreateFromImage(IEnumerable<byte> peImage)
        {
            return Create(ModuleMetadata.CreateFromImage(peImage));
        }

        /// <summary>
        /// Creates a single-module assembly.
        /// </summary>
        /// <param name="peStream">Manifest module PE image stream.</param>
        /// <param name="leaveOpen">False to close the stream upon disposal of the metadata.</param>
        /// <exception cref="BadImageFormatException">The PE image format is invalid.</exception>
        public static AssemblyMetadata CreateFromImageStream(Stream peStream, bool leaveOpen = false)
        {
            return Create(ModuleMetadata.CreateFromImageStream(peStream, leaveOpen));
        }

        /// <summary>
        /// Creates a single-module assembly.
        /// </summary>
        /// <param name="peStream">Manifest module PE image stream.</param>
        /// <param name="options">False to close the stream upon disposal of the metadata.</param>
        /// <exception cref="BadImageFormatException">The PE image format is invalid.</exception>
        public static AssemblyMetadata CreateFromImageStream(Stream peStream, PEStreamOptions options)
        {
            return Create(ModuleMetadata.CreateFromImageStream(peStream, options));
        }

        /// <summary>
        /// Creates a single-module assembly.
        /// </summary>
        /// <param name="module">
        /// Manifest module.
        /// </param>
        /// <remarks>This object disposes <paramref name="module"/> it when it is itself disposed.</remarks>
        public static AssemblyMetadata Create(ModuleMetadata module)
        {
            if (module == null)
            {
                throw new ArgumentNullException("module");
            }

            return new AssemblyMetadata(module);
        }

        /// <summary>
        /// Creates a multi-module assembly.
        /// </summary>
        /// <param name="modules">
        /// Modules comprising the assembly. The first module is the manifest module of the assembly.</param>
        /// <remarks>This object disposes the elements of <paramref name="modules"/> it when it is itself <see cref="Dispose"/>.</remarks>
        /// <exception cref="ArgumentException"><paramref name="modules"/> is default value.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="modules"/> contains null elements.</exception>
        /// <exception cref="ArgumentException"><paramref name="modules"/> is empty or contains a module that doesn't own its image (was created via <see cref="M:MetadataModule.Copy"/>).</exception>
        public static AssemblyMetadata Create(ImmutableArray<ModuleMetadata> modules)
        {
            if (modules.IsDefault)
            {
                throw new ArgumentException("modules");
            }

            if (modules.Length == 0)
            {
                throw new ArgumentException(CodeAnalysisResources.AssemblyMustHaveAtLeastOneModule, "modules");
            }

            for (int i = 0; i < modules.Length; i++)
            {
                if (modules[i] == null)
                {
                    throw new ArgumentNullException("modules[" + i + "]");
                }

                if (!modules[i].IsImageOwner)
                {
                    throw new ArgumentException(CodeAnalysisResources.ModuleCopyCannotBeUsedToCreateAssemblyMetadata, "modules[" + i + "]");
                }
            }

            return new AssemblyMetadata(modules);
        }

        /// <summary>
        /// Creates a multi-module assembly.
        /// </summary>
        /// <param name="modules">
        /// Modules comprising the assembly. The first module is the manifest module of the assembly.</param>
        /// <remarks>This object disposes the elements of <paramref name="modules"/> it when it is itself <see cref="Dispose"/>.</remarks>
        /// <exception cref="ArgumentException"><paramref name="modules"/> is default value.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="modules"/> contains null elements.</exception>
        /// <exception cref="ArgumentException"><paramref name="modules"/> is empty or contains a module that doesn't own its image (was created via <see cref="M:MetadataModule.Copy"/>).</exception>
        public static AssemblyMetadata Create(IEnumerable<ModuleMetadata> modules)
        {
            return Create(modules.AsImmutableOrNull());
        }

        /// <summary>
        /// Creates a multi-module assembly.
        /// </summary>
        /// <param name="modules">Modules comprising the assembly. The first module is the manifest module of the assembly.</param>
        /// <remarks>This object disposes the elements of <paramref name="modules"/> it when it is itself <see cref="Dispose"/>.</remarks>
        /// <exception cref="ArgumentException"><paramref name="modules"/> is default value.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="modules"/> contains null elements.</exception>
        /// <exception cref="ArgumentException"><paramref name="modules"/> is empty or contains a module that doesn't own its image (was created via <see cref="M:MetadataModule.Copy"/>).</exception>
        public static AssemblyMetadata Create(params ModuleMetadata[] modules)
        {
            return Create(ImmutableArray.CreateRange(modules));
        }

        /// <summary>
        /// Creates a shallow copy of contained modules and wraps them into a new instance of <see cref="AssemblyMetadata"/>.
        /// </summary>
        /// <remarks>
        /// The resulting copy shares the metadata images and metadata information read from them with the original.
        /// It doesn't own the underlying metadata images and is not responsible for its disposal.
        /// 
        /// This is used, for example, when a metadata cache needs to return the cached metadata to its users
        /// while keeping the ownership of the cached metadata object.
        /// </remarks>
        internal new AssemblyMetadata Copy()
        {
            return new AssemblyMetadata(this);
        }

        protected override Metadata CommonCopy()
        {
            return Copy();
        }

        /// <summary>
        /// Disposes all modules contained in the assembly.
        /// </summary>
        public override void Dispose()
        {
            foreach (var module in Modules)
            {
                module.Dispose();
            }
        }

        /// <summary>
        /// Checks if the first module has a single row in Assembly table and that all other modules have none.
        /// </summary>
        internal bool IsValidAssembly()
        {
            if (!ManifestModule.Module.IsManifestModule)
            {
                return false;
            }

            for (int i = 1; i < Modules.Length; i++)
            {
                if (!Modules[i].Module.IsLinkedModule)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// The manifest module of the assembly.
        /// </summary>
        public ModuleMetadata ManifestModule
        {
            get { return Modules[0]; }
        }

        /// <summary>
        /// Returns the metadata kind. <seealso cref="MetadataImageKind"/>
        /// </summary>
        public override MetadataImageKind Kind
        {
            get { return MetadataImageKind.Assembly; }
        }

        internal override bool IsImageOwner
        {
            get { return ManifestModule.IsImageOwner; }
        }
    }
}
