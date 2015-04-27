// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Scripting
{
    /// <summary>
    /// Represents a runtime execution context for C# scripts.
    /// </summary>
    internal abstract class ScriptEngine
    {
        public static readonly ImmutableArray<string> DefaultReferenceSearchPaths;

        // state captured by session at creation time:
        private ScriptOptions _options = ScriptOptions.Default;
        private ScriptBuilder _builder;

        static ScriptEngine()
        {
            DefaultReferenceSearchPaths = ImmutableArray.Create<string>(RuntimeEnvironment.GetRuntimeDirectory());
        }

        internal ScriptEngine(MetadataFileReferenceProvider metadataReferenceProvider, AssemblyLoader assemblyLoader)
        {
            if (metadataReferenceProvider == null)
            {
                metadataReferenceProvider = _options.AssemblyResolver.Provider;
            }

            if (assemblyLoader == null)
            {
                assemblyLoader = new InteractiveAssemblyLoader();
            }

            _builder = new ScriptBuilder(assemblyLoader);

            _options = _options.WithReferenceProvider(metadataReferenceProvider);

            string initialBaseDirectory;
            try
            {
                initialBaseDirectory = Directory.GetCurrentDirectory();
            }
            catch
            {
                initialBaseDirectory = null;
            }

            _options = _options.WithBaseDirectory(initialBaseDirectory);
        }

        public MetadataFileReferenceProvider MetadataReferenceProvider
        {
            get { return _options.AssemblyResolver.Provider; }
        }

        public AssemblyLoader AssemblyLoader
        {
            get { return _builder.AssemblyLoader; }
        }

        internal ScriptBuilder Builder
        {
            get { return _builder; }
        }

        // TODO (tomat): Consider exposing FileResolver and removing BaseDirectory.
        // We would need WithAssemblySearchPaths on FileResolver to implement SetReferenceSearchPaths 
        internal MetadataFileReferenceResolver MetadataReferenceResolver
        {
            get
            {
                return _options.AssemblyResolver.PathResolver;
            }

            // for testing
            set
            {
                Debug.Assert(value != null);
                _options = _options.WithReferenceResolver(value);
            }
        }

        internal string AssemblyNamePrefix
        {
            get { return _builder.AssemblyNamePrefix; }
        }

        #region Script
        internal abstract Script Create(string code, ScriptOptions options, Type globalsType, Type returnType);
        #endregion

        #region Session

        // for testing only:
        // TODO (tomat): Sessions generate uncollectible code since we don't know whether they would execute just one submissions or multiple.
        // We need to address code collectibility of multi-submission sessions at some point (the CLR needs to be fixed). Meanwhile use this helper 
        // to force collectible code generation in order to keep test coverage.
        internal Session CreateCollectibleSession()
        {
            return new Session(this, _options.WithIsCollectible(true), null);
        }

        internal Session CreateCollectibleSession<THostObject>(THostObject hostObject)
        {
            return new Session(this, _options.WithIsCollectible(true), hostObject, typeof(THostObject));
        }

        public Session CreateSession()  // TODO (tomat): bool isCancellable = false
        {
            return new Session(this, _options, null);
        }

        public Session CreateSession(object hostObject) // TODO (tomat): bool isCancellable = false
        {
            if (hostObject == null)
            {
                throw new ArgumentNullException(nameof(hostObject));
            }

            return new Session(this, _options, hostObject, hostObject.GetType());
        }

        public Session CreateSession(object hostObject, Type hostObjectType) // TODO (tomat): bool isCancellable = false
        {
            if (hostObject == null)
            {
                throw new ArgumentNullException(nameof(hostObject));
            }

            if (hostObjectType == null)
            {
                throw new ArgumentNullException(nameof(hostObjectType));
            }

            Type actualType = hostObject.GetType();
            if (!hostObjectType.IsAssignableFrom(actualType))
            {
                throw new ArgumentException(String.Format(ScriptingResources.CantAssignTo, actualType, hostObjectType), "hostObjectType");
            }

            return new Session(this, _options, hostObject, hostObjectType);
        }

        public Session CreateSession<THostObject>(THostObject hostObject) // TODO (tomat): bool isCancellable = false
            where THostObject : class
        {
            if (hostObject == null)
            {
                throw new ArgumentNullException(nameof(hostObject));
            }

            return new Session(this, _options, hostObject, typeof(THostObject));
        }

        #endregion

        #region State

        /// <summary>
        /// The base directory used to resolve relative paths to assembly references and 
        /// relative paths that appear in source code compiled by this script engine.
        /// </summary>
        /// <remarks>
        /// If null relative paths won't be resolved and an error will be reported when the compiler encountrs such paths.
        /// The value can be changed at any point in time. However the new value doesn't affect already compiled submissions.
        /// The initial value is the current working directory if the current process, or null if not available.
        /// Changing the base directory doesn't affect the process current working directory used by <see cref="System.IO"/> APIs.
        /// </remarks>
        public string BaseDirectory
        {
            get
            {
                return _options.BaseDirectory;
            }

            set
            {
                _options = _options.WithBaseDirectory(value);
            }
        }

        public ImmutableArray<string> ReferenceSearchPaths
        {
            get { return _options.SearchPaths; }
        }

        public void SetReferenceSearchPaths(params string[] paths)
        {
            SetReferenceSearchPaths(ImmutableArray.CreateRange<string>(paths));
        }

        public void SetReferenceSearchPaths(IEnumerable<string> paths)
        {
            SetReferenceSearchPaths(ImmutableArray.CreateRange<string>(paths));
        }

        public void SetReferenceSearchPaths(ImmutableArray<string> paths)
        {
            MetadataFileReferenceResolver.ValidateSearchPaths(paths, "paths");
            _options = _options.WithSearchPaths(paths);
        }

        /// <summary>
        /// Returns a list of assemblies that are currently referenced by the engine.
        /// </summary>
        public ImmutableArray<MetadataReference> GetReferences()
        {
            return _options.References;
        }

        /// <summary>
        /// Adds a reference to specified assembly.
        /// </summary>
        /// <param name="assemblyDisplayNameOrPath">Assembly display name or path.</param>
        /// <exception cref="ArgumentNullException"><paramref name="assemblyDisplayNameOrPath"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="assemblyDisplayNameOrPath"/> is empty.</exception>
        /// <exception cref="FileNotFoundException">Assembly file can't be found.</exception>
        public void AddReference(string assemblyDisplayNameOrPath)
        {
            if (assemblyDisplayNameOrPath == null)
            {
                throw new ArgumentNullException(nameof(assemblyDisplayNameOrPath));
            }

            _options = _options.AddReferences(assemblyDisplayNameOrPath);
        }

        /// <summary>
        /// Adds a reference to specified assembly.
        /// </summary>
        /// <param name="assembly">Runtime assembly. The assembly must be loaded from a file on disk. In-memory assemblies are not supported.</param>
        /// <exception cref="ArgumentNullException"><paramref name="assembly"/> is null.</exception>
        public void AddReference(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            _options = _options.AddReferences(assembly);
        }

        /// <summary>
        /// Adds a reference to specified assembly.
        /// </summary>
        /// <param name="reference">Assembly reference.</param>
        /// <exception cref="ArgumentException"><paramref name="reference"/> is not an assembly reference (it's a module).</exception>
        /// <exception cref="ArgumentNullException"><paramref name="reference"/> is null.</exception>
        public void AddReference(MetadataReference reference)
        {
            if (reference == null)
            {
                throw new ArgumentNullException(nameof(reference));
            }

            if (reference.Properties.Kind != MetadataImageKind.Assembly)
            {
                throw new ArgumentException(ScriptingResources.ExpectedAnAssemblyReference, nameof(reference));
            }

            _options = _options.AddReferences(reference);
        }

        /// <summary>
        /// Returns a list of imported namespaces.
        /// </summary>
        public ImmutableArray<string> GetImportedNamespaces()
        {
            return _options.Namespaces;
        }

        /// <summary>
        /// Imports a namespace, an equivalent of executing "using <paramref name="namespace"/>;" (C#) or "Imports <paramref name="namespace"/>" (VB).
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="namespace"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="namespace"/> is not a valid namespace name.</exception>
        public void ImportNamespace(string @namespace)
        {
            ValidateNamespace(@namespace);

            // we don't report duplicates to get the same behavior as evaluating "using NS;" twice.
            _options = _options.AddNamespaces(@namespace);
        }

        internal static void ValidateNamespace(string @namespace)
        {
            if (@namespace == null)
            {
                throw new ArgumentNullException(nameof(@namespace));
            }

            // Only check that the namespace is a CLR namespace name.
            // If the namespace doesn't exist an error will be reported when compiling the next submission.

            if (!@namespace.IsValidClrNamespaceName())
            {
                throw new ArgumentException("Invalid namespace name", nameof(@namespace));
            }
        }

        #endregion
    }
}
