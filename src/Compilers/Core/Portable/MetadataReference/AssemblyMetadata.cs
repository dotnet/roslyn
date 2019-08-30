// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents an immutable snapshot of assembly CLI metadata.
    /// </summary>
    public sealed class AssemblyMetadata : Metadata
    {
        private sealed class Data
        {
            public static readonly Data Disposed = new Data();

            public readonly ImmutableArray<ModuleMetadata> Modules;
            public readonly PEAssembly Assembly;

            private Data()
            {
            }

            public Data(ImmutableArray<ModuleMetadata> modules, PEAssembly assembly)
            {
                Debug.Assert(!modules.IsDefaultOrEmpty && assembly != null);

                this.Modules = modules;
                this.Assembly = assembly;
            }

            public bool IsDisposed
            {
                get { return Assembly == null; }
            }
        }

        /// <summary>
        /// Factory that provides the <see cref="ModuleMetadata"/> for additional modules (other than <see cref="_initialModules"/>) of the assembly.
        /// Shall only throw <see cref="BadImageFormatException"/> or <see cref="IOException"/>.
        /// Null of all modules were specified at construction time.
        /// </summary>
        private readonly Func<string, ModuleMetadata> _moduleFactoryOpt;

        /// <summary>
        /// Modules the <see cref="AssemblyMetadata"/> was created with, in case they are eagerly allocated.
        /// </summary>
        private readonly ImmutableArray<ModuleMetadata> _initialModules;

        // Encapsulates the modules and the corresponding PEAssembly produced by the modules factory.
        private Data _lazyData;

        // The actual array of modules exposed via Modules property.
        // The same modules as the ones produced by the factory or their copies.
        private ImmutableArray<ModuleMetadata> _lazyPublishedModules;

        /// <summary>
        /// Cached assembly symbols.
        /// </summary>
        /// <remarks>
        /// Guarded by <see cref="CommonReferenceManager.SymbolCacheAndReferenceManagerStateGuard"/>.
        /// </remarks>
        internal readonly WeakList<IAssemblySymbol> CachedSymbols = new WeakList<IAssemblySymbol>();

        // creates a copy
        private AssemblyMetadata(AssemblyMetadata other, bool shareCachedSymbols)
            : base(isImageOwner: false, id: other.Id)
        {
            if (shareCachedSymbols)
            {
                this.CachedSymbols = other.CachedSymbols;
            }

            _lazyData = other._lazyData;
            _moduleFactoryOpt = other._moduleFactoryOpt;
            _initialModules = other._initialModules;
            // Leave lazyPublishedModules unset. Published modules will be set and copied as needed.
        }

        internal AssemblyMetadata(ImmutableArray<ModuleMetadata> modules)
            : base(isImageOwner: true, id: MetadataId.CreateNewId())
        {
            Debug.Assert(!modules.IsDefaultOrEmpty);
            _initialModules = modules;
        }

        internal AssemblyMetadata(ModuleMetadata manifestModule, Func<string, ModuleMetadata> moduleFactory)
            : base(isImageOwner: true, id: MetadataId.CreateNewId())
        {
            Debug.Assert(manifestModule != null);
            Debug.Assert(moduleFactory != null);

            _initialModules = ImmutableArray.Create(manifestModule);
            _moduleFactoryOpt = moduleFactory;
        }

        /// <summary>
        /// Creates a single-module assembly.
        /// </summary>
        /// <param name="peImage">
        /// Manifest module image.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="peImage"/> is null.</exception>
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
        /// <exception cref="ArgumentNullException"><paramref name="peImage"/> is null.</exception>
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
        public static AssemblyMetadata CreateFromStream(Stream peStream, bool leaveOpen = false)
        {
            return Create(ModuleMetadata.CreateFromStream(peStream, leaveOpen));
        }

        /// <summary>
        /// Creates a single-module assembly.
        /// </summary>
        /// <param name="peStream">Manifest module PE image stream.</param>
        /// <param name="options">False to close the stream upon disposal of the metadata.</param>
        /// <exception cref="BadImageFormatException">The PE image format is invalid.</exception>
        public static AssemblyMetadata CreateFromStream(Stream peStream, PEStreamOptions options)
        {
            return Create(ModuleMetadata.CreateFromStream(peStream, options));
        }

        /// <summary>
        /// Finds all modules of an assembly on a specified path and builds an instance of <see cref="AssemblyMetadata"/> that represents them.
        /// </summary>
        /// <param name="path">The full path to the assembly on disk.</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is invalid.</exception>
        /// <exception cref="IOException">Error reading file <paramref name="path"/>. See <see cref="Exception.InnerException"/> for details.</exception>
        /// <exception cref="NotSupportedException">Reading from a file path is not supported by the platform.</exception>
        public static AssemblyMetadata CreateFromFile(string path)
        {
            return CreateFromFile(ModuleMetadata.CreateFromFile(path), path);
        }

        internal static AssemblyMetadata CreateFromFile(ModuleMetadata manifestModule, string path)
        {
            return new AssemblyMetadata(manifestModule, moduleName => ModuleMetadata.CreateFromFile(Path.Combine(Path.GetDirectoryName(path), moduleName)));
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
                throw new ArgumentNullException(nameof(module));
            }

            return new AssemblyMetadata(ImmutableArray.Create(module));
        }

        /// <summary>
        /// Creates a multi-module assembly.
        /// </summary>
        /// <param name="modules">
        /// Modules comprising the assembly. The first module is the manifest module of the assembly.</param>
        /// <remarks>This object disposes the elements of <paramref name="modules"/> it when it is itself <see cref="Dispose"/>.</remarks>
        /// <exception cref="ArgumentException"><paramref name="modules"/> is default value.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="modules"/> contains null elements.</exception>
        /// <exception cref="ArgumentException"><paramref name="modules"/> is empty or contains a module that doesn't own its image (was created via <see cref="Metadata.Copy"/>).</exception>
        public static AssemblyMetadata Create(ImmutableArray<ModuleMetadata> modules)
        {
            if (modules.IsDefaultOrEmpty)
            {
                throw new ArgumentException(CodeAnalysisResources.AssemblyMustHaveAtLeastOneModule, nameof(modules));
            }

            for (int i = 0; i < modules.Length; i++)
            {
                if (modules[i] == null)
                {
                    throw new ArgumentNullException(nameof(modules) + "[" + i + "]");
                }

                if (!modules[i].IsImageOwner)
                {
                    throw new ArgumentException(CodeAnalysisResources.ModuleCopyCannotBeUsedToCreateAssemblyMetadata, nameof(modules) + "[" + i + "]");
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
        /// <exception cref="ArgumentException"><paramref name="modules"/> is empty or contains a module that doesn't own its image (was created via <see cref="Metadata.Copy"/>).</exception>
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
        /// <exception cref="ArgumentException"><paramref name="modules"/> is empty or contains a module that doesn't own its image (was created via <see cref="Metadata.Copy"/>).</exception>
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
            return new AssemblyMetadata(this, shareCachedSymbols: true);
        }

        internal AssemblyMetadata CopyWithoutSharingCachedSymbols()
        {
            return new AssemblyMetadata(this, shareCachedSymbols: false);
        }

        protected override Metadata CommonCopy()
        {
            return Copy();
        }

        /// <summary>
        /// Modules comprising this assembly. The first module is the manifest module.
        /// </summary>
        /// <exception cref="BadImageFormatException">The PE image format is invalid.</exception>
        /// <exception cref="IOException">IO error reading the metadata. See <see cref="Exception.InnerException"/> for details.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public ImmutableArray<ModuleMetadata> GetModules()
        {
            if (_lazyPublishedModules.IsDefault)
            {
                var data = GetOrCreateData();
                var newModules = data.Modules;

                if (!IsImageOwner)
                {
                    newModules = newModules.SelectAsArray(module => module.Copy());
                }

                ImmutableInterlocked.InterlockedInitialize(ref _lazyPublishedModules, newModules);
            }

            if (_lazyData == Data.Disposed)
            {
                throw new ObjectDisposedException(nameof(AssemblyMetadata));
            }

            return _lazyPublishedModules;
        }

        /// <exception cref="BadImageFormatException">The PE image format is invalid.</exception>
        /// <exception cref="IOException">IO error while reading the metadata. See <see cref="Exception.InnerException"/> for details.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        internal PEAssembly GetAssembly()
        {
            return GetOrCreateData().Assembly;
        }

        /// <exception cref="BadImageFormatException">The PE image format is invalid.</exception>
        /// <exception cref="IOException">IO error while reading the metadata. See <see cref="Exception.InnerException"/> for details.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        private Data GetOrCreateData()
        {
            if (_lazyData == null)
            {
                ImmutableArray<ModuleMetadata> modules = _initialModules;
                ImmutableArray<ModuleMetadata>.Builder moduleBuilder = null;

                bool createdModulesUsed = false;
                try
                {
                    if (_moduleFactoryOpt != null)
                    {
                        Debug.Assert(_initialModules.Length == 1);

                        var additionalModuleNames = _initialModules[0].GetModuleNames();
                        if (additionalModuleNames.Length > 0)
                        {
                            moduleBuilder = ImmutableArray.CreateBuilder<ModuleMetadata>(1 + additionalModuleNames.Length);
                            moduleBuilder.Add(_initialModules[0]);

                            foreach (string moduleName in additionalModuleNames)
                            {
                                moduleBuilder.Add(_moduleFactoryOpt(moduleName));
                            }

                            modules = moduleBuilder.ToImmutable();
                        }
                    }

                    var assembly = new PEAssembly(this, modules.SelectAsArray(m => m.Module));
                    var newData = new Data(modules, assembly);

                    createdModulesUsed = Interlocked.CompareExchange(ref _lazyData, newData, null) == null;
                }
                finally
                {
                    if (moduleBuilder != null && !createdModulesUsed)
                    {
                        // dispose unused modules created above:
                        for (int i = _initialModules.Length; i < moduleBuilder.Count; i++)
                        {
                            moduleBuilder[i].Dispose();
                        }
                    }
                }
            }

            if (_lazyData.IsDisposed)
            {
                throw new ObjectDisposedException(nameof(AssemblyMetadata));
            }

            return _lazyData;
        }

        /// <summary>
        /// Disposes all modules contained in the assembly.
        /// </summary>
        public override void Dispose()
        {
            var previousData = Interlocked.Exchange(ref _lazyData, Data.Disposed);

            if (previousData == Data.Disposed || !this.IsImageOwner)
            {
                // already disposed, or not an owner
                return;
            }

            // AssemblyMetadata assumes their ownership of all modules passed to the constructor.
            foreach (var module in _initialModules)
            {
                module.Dispose();
            }

            if (previousData == null)
            {
                // no additional modules were created yet => nothing to dispose
                return;
            }

            Debug.Assert(_initialModules.Length == 1 || _initialModules.Length == previousData.Modules.Length);

            // dispose additional modules created lazily:
            for (int i = _initialModules.Length; i < previousData.Modules.Length; i++)
            {
                previousData.Modules[i].Dispose();
            }
        }

        /// <summary>
        /// Checks if the first module has a single row in Assembly table and that all other modules have none.
        /// </summary>
        /// <exception cref="BadImageFormatException">The PE image format is invalid.</exception>
        /// <exception cref="IOException">IO error reading the metadata. See <see cref="Exception.InnerException"/> for details.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        internal bool IsValidAssembly()
        {
            var modules = GetModules();

            if (!modules[0].Module.IsManifestModule)
            {
                return false;
            }

            for (int i = 1; i < modules.Length; i++)
            {
                // Ignore winmd modules since runtime winmd modules may be loaded as non-primary modules.
                var module = modules[i].Module;
                if (!module.IsLinkedModule && module.MetadataReader.MetadataKind != MetadataKind.WindowsMetadata)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns the metadata kind. <seealso cref="MetadataImageKind"/>
        /// </summary>
        public override MetadataImageKind Kind
        {
            get { return MetadataImageKind.Assembly; }
        }

        /// <summary>
        /// Creates a reference to the assembly metadata.
        /// </summary>
        /// <param name="documentation">Provider of XML documentation comments for the metadata symbols contained in the module.</param>
        /// <param name="aliases">Aliases that can be used to refer to the assembly from source code (see "extern alias" directive in C#).</param>
        /// <param name="embedInteropTypes">True to embed interop types from the referenced assembly to the referencing compilation. Must be false for a module.</param>
        /// <param name="filePath">Path describing the location of the metadata, or null if the metadata have no location.</param>
        /// <param name="display">Display string used in error messages to identity the reference.</param>
        /// <returns>A reference to the assembly metadata.</returns>
        public PortableExecutableReference GetReference(
            DocumentationProvider documentation = null,
            ImmutableArray<string> aliases = default(ImmutableArray<string>),
            bool embedInteropTypes = false,
            string filePath = null,
            string display = null)
        {
            return new MetadataImageReference(this, new MetadataReferenceProperties(MetadataImageKind.Assembly, aliases, embedInteropTypes), documentation, filePath, display);
        }
    }
}
