// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Scripting
{
    /// <summary>
    /// Options for creating and running scripts.
    /// </summary>
    public class ScriptOptions
    {
        private readonly ImmutableArray<MetadataReference> _references;
        private readonly ImmutableArray<string> _namespaces;
        private readonly AssemblyReferenceResolver _referenceResolver;
        private readonly bool _isCollectible;
        private readonly bool _isInteractive;

        public ScriptOptions()
            : this(ImmutableArray<MetadataReference>.Empty,
                  ImmutableArray<string>.Empty,
                  new AssemblyReferenceResolver(GacFileResolver.Default, MetadataFileReferenceProvider.Default),
                  isInteractive: true,
                  isCollectible: false)
        {
        }

        public static readonly ScriptOptions Default;

        static ScriptOptions()
        {
            var paths = ImmutableArray.Create(RuntimeEnvironment.GetRuntimeDirectory());

            Default = new ScriptOptions()
                        .WithReferences(typeof(int).Assembly)
                        .WithNamespaces("System")
                        .WithSearchPaths(paths);
        }

        private ScriptOptions(
            ImmutableArray<MetadataReference> references,
            ImmutableArray<string> namespaces,
            AssemblyReferenceResolver referenceResolver,
            bool isInteractive,
            bool isCollectible)
        {
            _references = references;
            _namespaces = namespaces;
            _referenceResolver = referenceResolver;
            _isInteractive = isInteractive;
            _isCollectible = isCollectible;
        }

        /// <summary>
        /// The set of <see cref="MetadataReference"/>'s used by the script.
        /// </summary>
        public ImmutableArray<MetadataReference> References
        {
            get { return _references; }
        }

        /// <summary>
        /// The namespaces automatically imported by the script.
        /// </summary>
        public ImmutableArray<string> Namespaces
        {
            get { return _namespaces; }
        }

        /// <summary>
        /// The paths used when searching for references.
        /// </summary>
        public ImmutableArray<string> SearchPaths
        {
            get { return _referenceResolver.PathResolver.SearchPaths; }
        }

        /// <summary>
        /// The base directory used when searching for references.
        /// </summary>
        public string BaseDirectory
        {
            get { return _referenceResolver.PathResolver.BaseDirectory; }
        }

        /// <summary>
        /// The <see cref="MetadataFileReferenceProvider"/> scripts will use to translate assembly names into metadata file paths. (#r syntax)
        /// </summary>
        public MetadataReferenceResolver ReferenceResolver
        {
            get { return _referenceResolver; }
        }

        // TODO:
        internal AssemblyReferenceResolver AssemblyResolver
        {
            get { return _referenceResolver; }
        }

        internal MetadataFileReferenceResolver FileReferenceResolver
        {
            get { return _referenceResolver.PathResolver; }
        }

        /// <summary>
        /// True if the script is interactive. 
        /// Interactive scripts may contain a final expression whose value is returned when the script is run.
        /// </summary>
        public bool IsInteractive
        {
            get { return _isInteractive; }
        }

        internal bool IsCollectible
        {
            get { return _isCollectible; }
        }

        private ScriptOptions With(
            Optional<ImmutableArray<MetadataReference>> references = default(Optional<ImmutableArray<MetadataReference>>),
            Optional<ImmutableArray<string>> namespaces = default(Optional<ImmutableArray<string>>),
            Optional<AssemblyReferenceResolver> resolver = default(Optional<AssemblyReferenceResolver>),
            Optional<bool> isInteractive = default(Optional<bool>),
            Optional<bool> isCollectible = default(Optional<bool>))
        {
            var newReferences = references.HasValue ? references.Value : _references;
            var newNamespaces = namespaces.HasValue ? namespaces.Value : _namespaces;
            var newResolver = resolver.HasValue ? resolver.Value : _referenceResolver;
            var newIsInteractive = isInteractive.HasValue ? isInteractive.Value : _isInteractive;
            var newIsCollectible = isCollectible.HasValue ? isCollectible.Value : _isCollectible;

            if (newReferences == _references &&
                newNamespaces == _namespaces &&
                newResolver == _referenceResolver &&
                newIsInteractive == _isInteractive &&
                newIsCollectible == _isCollectible)
            {
                return this;
            }
            else
            {
                return new ScriptOptions(newReferences, newNamespaces, newResolver, newIsInteractive, newIsCollectible);
            }
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the references changed.
        /// </summary>
        public ScriptOptions WithReferences(ImmutableArray<MetadataReference> references)
        {
            return With(references: references.IsDefault ? ImmutableArray<MetadataReference>.Empty : references);
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the references changed.
        /// </summary>
        public ScriptOptions WithReferences(IEnumerable<MetadataReference> references)
        {
            return WithReferences(references != null ? references.ToImmutableArray() : ImmutableArray<MetadataReference>.Empty);
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the references changed.
        /// </summary>
        public ScriptOptions WithReferences(params MetadataReference[] references)
        {
            return WithReferences((IEnumerable<MetadataReference>)references);
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with references added.
        /// </summary>
        public ScriptOptions AddReferences(IEnumerable<MetadataReference> references)
        {
            if (_references == null)
            {
                return this;
            }
            else
            {
                return this.WithReferences(this.References.AddRange(references.Where(r => r != null && !this.References.Contains(r))));
            }
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with references added.
        /// </summary>
        public ScriptOptions AddReferences(params MetadataReference[] references)
        {
            return AddReferences((IEnumerable<MetadataReference>)references);
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the references changed.
        /// </summary>
        public ScriptOptions WithReferences(IEnumerable<System.Reflection.Assembly> assemblies)
        {
            if (assemblies == null)
            {
                return WithReferences((IEnumerable<MetadataReference>)null);
            }
            else
            {
                return WithReferences(assemblies.Select(MetadataReference.CreateFromAssemblyInternal));
            }
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the references changed.
        /// </summary>
        public ScriptOptions WithReferences(params System.Reflection.Assembly[] assemblies)
        {
            return WithReferences((IEnumerable<System.Reflection.Assembly>)assemblies);
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with references added.
        /// </summary>
        public ScriptOptions AddReferences(IEnumerable<System.Reflection.Assembly> assemblies)
        {
            if (assemblies == null)
            {
                return this;
            }
            else
            {
                return AddReferences(assemblies.Select(MetadataReference.CreateFromAssemblyInternal));
            }
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with references added.
        /// </summary>
        public ScriptOptions AddReferences(params System.Reflection.Assembly[] assemblies)
        {
            return AddReferences((IEnumerable<System.Reflection.Assembly>)assemblies);
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the references changed.
        /// </summary>
        public ScriptOptions WithReferences(IEnumerable<string> references)
        {
            if (references == null)
            {
                return WithReferences(ImmutableArray<MetadataReference>.Empty);
            }
            else
            {
                return WithReferences(references.Where(name => name != null).Select(name => ResolveReference(name)));
            }
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the references changed.
        /// </summary>
        public ScriptOptions WithReferences(params string[] references)
        {
            return WithReferences((IEnumerable<string>)references);
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with references added.
        /// </summary>
        public ScriptOptions AddReferences(IEnumerable<string> references)
        {
            if (references == null)
            {
                return this;
            }
            else
            {
                return AddReferences(references.Where(name => name != null).Select(name => ResolveReference(name)));
            }
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with references added.
        /// </summary>
        public ScriptOptions AddReferences(params string[] references)
        {
            return AddReferences((IEnumerable<string>)references);
        }

        private MetadataReference ResolveReference(string assemblyDisplayNameOrPath)
        {
            // TODO:
            string fullPath = _referenceResolver.PathResolver.ResolveReference(assemblyDisplayNameOrPath, baseFilePath: null);
            if (fullPath == null)
            {
                throw new System.IO.FileNotFoundException(ScriptingResources.AssemblyNotFound, assemblyDisplayNameOrPath);
            }

            return _referenceResolver.Provider.GetReference(fullPath, MetadataReferenceProperties.Assembly);
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the namespaces changed.
        /// </summary>
        public ScriptOptions WithNamespaces(ImmutableArray<string> namespaces)
        {
            return With(namespaces: namespaces.IsDefault ? ImmutableArray<string>.Empty : namespaces);
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the namespaces changed.
        /// </summary>
        public ScriptOptions WithNamespaces(IEnumerable<string> namespaces)
        {
            return WithNamespaces(namespaces != null ? namespaces.ToImmutableArray() : ImmutableArray<string>.Empty);
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the namespaces changed.
        /// </summary>
        public ScriptOptions WithNamespaces(params string[] namespaces)
        {
            return WithNamespaces((IEnumerable<string>)namespaces);
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with namespaces added.
        /// </summary>
        public ScriptOptions AddNamespaces(IEnumerable<string> namespaces)
        {
            if (namespaces == null)
            {
                return this;
            }
            else
            {
                return this.WithNamespaces(this.Namespaces.AddRange(namespaces.Where(n => n != null && !this.Namespaces.Contains(n))));
            }
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with namespaces added.
        /// </summary>
        public ScriptOptions AddNamespaces(params string[] namespaces)
        {
            return AddNamespaces((IEnumerable<string>)namespaces);
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the search paths changed.
        /// </summary>
        public ScriptOptions WithSearchPaths(IEnumerable<string> searchPaths)
        {
            if (this.SearchPaths.SequenceEqual(searchPaths))
            {
                return this;
            }
            else
            {
                // TODO:
                var gacResolver = _referenceResolver.PathResolver as GacFileResolver;
                if (gacResolver != null)
                {
                    return With(resolver: new AssemblyReferenceResolver(
                        new GacFileResolver(
                            searchPaths,
                            gacResolver.BaseDirectory,
                            gacResolver.Architectures,
                            gacResolver.PreferredCulture),
                        _referenceResolver.Provider));
                }
                else
                {
                    return With(resolver: new AssemblyReferenceResolver(
                        new MetadataFileReferenceResolver(
                            searchPaths,
                            _referenceResolver.PathResolver.BaseDirectory),
                        _referenceResolver.Provider));
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the search paths changed.
        /// </summary>
        public ScriptOptions WithSearchPaths(params string[] searchPaths)
        {
            return WithSearchPaths((IEnumerable<string>)searchPaths);
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with search paths added.
        /// </summary>
        public ScriptOptions AddSearchPaths(params string[] searchPaths)
        {
            return AddSearchPaths((IEnumerable<string>)searchPaths);
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with search paths added.
        /// </summary>
        public ScriptOptions AddSearchPaths(IEnumerable<string> searchPaths)
        {
            if (searchPaths == null)
            {
                return this;
            }
            else
            {
                return WithSearchPaths(this.SearchPaths.AddRange(searchPaths.Where(s => s != null && !this.SearchPaths.Contains(s))));
            }
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the base directory changed.
        /// </summary>
        public ScriptOptions WithBaseDirectory(string baseDirectory)
        {
            if (this.BaseDirectory == baseDirectory)
            {
                return this;
            }
            else
            {
                // TODO:
                var gacResolver = _referenceResolver.PathResolver as GacFileResolver;
                if (gacResolver != null)
                {
                    return With(resolver: new AssemblyReferenceResolver(
                        new GacFileResolver(
                            _referenceResolver.PathResolver.SearchPaths,
                            baseDirectory,
                            gacResolver.Architectures,
                            gacResolver.PreferredCulture),
                        _referenceResolver.Provider));
                }
                else
                {
                    return With(resolver: new AssemblyReferenceResolver(
                        new MetadataFileReferenceResolver(
                            _referenceResolver.PathResolver.SearchPaths,
                            baseDirectory),
                        _referenceResolver.Provider));
                }
            }
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the reference resolver specified.
        /// </summary>
        internal ScriptOptions WithReferenceResolver(MetadataFileReferenceResolver resolver)
        {
            if (resolver == _referenceResolver.PathResolver)
            {
                return this;
            }

            return With(resolver: new AssemblyReferenceResolver(resolver, _referenceResolver.Provider));
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the reference provider specified.
        /// </summary>
        internal ScriptOptions WithReferenceProvider(MetadataFileReferenceProvider provider)
        {
            if (provider == _referenceResolver.Provider)
            {
                return this;
            }

            return With(resolver: new AssemblyReferenceResolver(_referenceResolver.PathResolver, provider));
        }

        /// <summary>
        /// Create a new <see cref="ScriptOptions"/> with the interactive state specified.
        /// Interactive scripts may contain a final expression whose value is returned when the script is run.
        /// </summary>
        public ScriptOptions WithIsInteractive(bool isInteractive)
        {
            return With(isInteractive: isInteractive);
        }

        internal ScriptOptions WithIsCollectible(bool isCollectible)
        {
            return With(isCollectible: isCollectible);
        }
    }
}
