// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Scripting
{
    /// <summary>
    /// Options for creating and running scripts.
    /// </summary>
    public sealed class ScriptOptions
    {
        public static readonly ScriptOptions Default = new ScriptOptions();

        private readonly AssemblyReferenceResolver _referenceResolver;

        public ScriptOptions()
            : this("",
                  ImmutableArray<MetadataReference>.Empty,
                  ImmutableArray<string>.Empty,
                  new AssemblyReferenceResolver(
                      new DesktopMetadataReferenceResolver(
                          MetadataFileReferenceResolver.Default,
                          null,
                          GacFileResolver.Default),
                      MetadataFileReferenceProvider.Default),
                  isInteractive: true)
        {
        }

        private ScriptOptions(
            string path,
            ImmutableArray<MetadataReference> references,
            ImmutableArray<string> namespaces,
            AssemblyReferenceResolver referenceResolver,
            bool isInteractive)
        {
            Path = path;
            References = references;
            Namespaces = namespaces;
            _referenceResolver = referenceResolver;
            IsInteractive = isInteractive;
        }

        /// <summary>
        /// The set of <see cref="MetadataReference"/>'s used by the script.
        /// </summary>
        public ImmutableArray<MetadataReference> References { get; }

        /// <summary>
        /// The namespaces automatically imported by the script.
        /// </summary>
        public ImmutableArray<string> Namespaces { get; }

        /// <summary>
        /// The paths used when searching for references.
        /// </summary>
        public ImmutableArray<string> SearchPaths => _referenceResolver.PathResolver.SearchPaths;

        /// <summary>
        /// The base directory used when searching for references.
        /// </summary>
        public string BaseDirectory => _referenceResolver.PathResolver.BaseDirectory;

        /// <summary>
        /// The path to the script source if it originated from a file, empty otherwise.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// The <see cref="MetadataFileReferenceProvider"/> scripts will use to translate assembly names into metadata file paths. (#r syntax)
        /// </summary>
        public MetadataReferenceResolver ReferenceResolver => _referenceResolver;

        // TODO:
        internal AssemblyReferenceResolver AssemblyResolver => _referenceResolver;
        internal MetadataFileReferenceResolver FileReferenceResolver => _referenceResolver.PathResolver;

        /// <summary>
        /// True if the script is interactive. 
        /// Interactive scripts may contain a final expression whose value is returned when the script is run.
        /// </summary>
        public bool IsInteractive { get; }

        private ScriptOptions With(
            Optional<string> path = default(Optional<string>),
            Optional<ImmutableArray<MetadataReference>> references = default(Optional<ImmutableArray<MetadataReference>>),
            Optional<ImmutableArray<string>> namespaces = default(Optional<ImmutableArray<string>>),
            Optional<AssemblyReferenceResolver> resolver = default(Optional<AssemblyReferenceResolver>),
            Optional<bool> isInteractive = default(Optional<bool>))
        {
            var newPath = path.HasValue ? path.Value : Path;
            var newReferences = references.HasValue ? references.Value : References;
            var newNamespaces = namespaces.HasValue ? namespaces.Value : Namespaces;
            var newResolver = resolver.HasValue ? resolver.Value : _referenceResolver;
            var newIsInteractive = isInteractive.HasValue ? isInteractive.Value : IsInteractive;

            if (newPath == Path &&
                newReferences == References &&
                newNamespaces == Namespaces &&
                newResolver == _referenceResolver &&
                newIsInteractive == IsInteractive)
            {
                return this;
            }

            return new ScriptOptions(newPath, newReferences, newNamespaces, newResolver, newIsInteractive);
        }
        
        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the <see cref="Path"/> changed.
        /// </summary>
        public ScriptOptions WithPath(string path) =>
            With(path: path ?? "");

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the references changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        public ScriptOptions WithReferences(ImmutableArray<MetadataReference> references) => 
            With(references: ToImmutableArrayChecked(references, nameof(references)));

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the references changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        public ScriptOptions WithReferences(IEnumerable<MetadataReference> references) =>
            WithReferences(ToImmutableArrayChecked(references, nameof(references))); 

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the references changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        public ScriptOptions WithReferences(params MetadataReference[] references) =>
            WithReferences((IEnumerable<MetadataReference>)references);

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with references added.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        public ScriptOptions AddReferences(IEnumerable<MetadataReference> references) =>
            WithReferences(ConcatChecked(References, references, nameof(references)));

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with references added.
        /// </summary>
        public ScriptOptions AddReferences(params MetadataReference[] references) => 
            AddReferences((IEnumerable<MetadataReference>)references);

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the references changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        public ScriptOptions WithReferences(IEnumerable<Assembly> references) => 
            WithReferences(SelectChecked(references, nameof(references), MetadataReference.CreateFromAssemblyInternal));

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the references changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        public ScriptOptions WithReferences(params Assembly[] references) => 
            WithReferences((IEnumerable<Assembly>)references);

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with references added.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        public ScriptOptions AddReferences(IEnumerable<Assembly> references) =>
            AddReferences(SelectChecked(references, nameof(references), MetadataReference.CreateFromAssemblyInternal));

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with references added.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        public ScriptOptions AddReferences(params Assembly[] references) => 
            AddReferences((IEnumerable<Assembly>)references);

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the references changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        public ScriptOptions WithReferences(IEnumerable<string> references) => 
            WithReferences(SelectChecked(references, nameof(references), ResolveReference));

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the references changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        public ScriptOptions WithReferences(params string[] references) => 
            WithReferences((IEnumerable<string>)references);

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with references added.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        public ScriptOptions AddReferences(IEnumerable<string> references) => 
            AddReferences(SelectChecked(references, nameof(references), ResolveReference));

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with references added.
        /// </summary>
        public ScriptOptions AddReferences(params string[] references) => 
            AddReferences((IEnumerable<string>)references);

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
        /// <exception cref="ArgumentNullException"><paramref name="namespaces"/> is null or contains a null reference.</exception>
        public ScriptOptions WithNamespaces(ImmutableArray<string> namespaces) => 
            With(namespaces: ToImmutableArrayChecked(namespaces, nameof(namespaces)));

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the namespaces changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="namespaces"/> is null or contains a null reference.</exception>
        public ScriptOptions WithNamespaces(IEnumerable<string> namespaces) => 
            WithNamespaces(ToImmutableArrayChecked(namespaces, nameof(namespaces)));

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the namespaces changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="namespaces"/> is null or contains a null reference.</exception>
        public ScriptOptions WithNamespaces(params string[] namespaces) => 
            WithNamespaces((IEnumerable<string>)namespaces);

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with namespaces added.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="namespaces"/> is null or contains a null reference.</exception>
        public ScriptOptions AddNamespaces(IEnumerable<string> namespaces) => 
            WithNamespaces(ConcatChecked(Namespaces, namespaces, nameof(namespaces)));

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with namespaces added.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="namespaces"/> is null or contains a null reference.</exception>
        public ScriptOptions AddNamespaces(params string[] namespaces) => 
            AddNamespaces((IEnumerable<string>)namespaces);

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the search paths changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="searchPaths"/> is null or contains a null reference.</exception>
        public ScriptOptions WithSearchPaths(IEnumerable<string> searchPaths)
        {
            if (searchPaths != null && SearchPaths.SequenceEqual(searchPaths))
            {
                return this;
            }

            // TODO:
            var resolver = new AssemblyReferenceResolver(
                _referenceResolver.PathResolver.WithSearchPaths(ToImmutableArrayChecked(searchPaths, nameof(searchPaths))),
                _referenceResolver.Provider);

            return With(resolver: resolver);
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the search paths changed.
        /// </summary>
        public ScriptOptions WithSearchPaths(params string[] searchPaths) => 
            WithSearchPaths((IEnumerable<string>)searchPaths);

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with search paths added.
        /// </summary>
        public ScriptOptions AddSearchPaths(params string[] searchPaths) => 
            AddSearchPaths((IEnumerable<string>)searchPaths);

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with search paths added.
        /// </summary>
        public ScriptOptions AddSearchPaths(IEnumerable<string> searchPaths) => 
            WithSearchPaths(ConcatChecked(SearchPaths, searchPaths, nameof(searchPaths)));

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the base directory changed.
        /// </summary>
        /// <remarks>
        /// If null is specified relative paths won't be resolved.
        /// </remarks>
        public ScriptOptions WithBaseDirectory(string baseDirectory)
        {
            if (BaseDirectory == baseDirectory)
            {
                return this;
            }

            // TODO:
            var resolver = new AssemblyReferenceResolver(
                _referenceResolver.PathResolver.WithBaseDirectory(baseDirectory),
                _referenceResolver.Provider);

            return With(resolver: resolver);
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the reference resolver specified.
        /// </summary>
        internal ScriptOptions WithReferenceResolver(MetadataFileReferenceResolver resolver)
        {
            if (resolver.Equals(_referenceResolver.PathResolver))
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
            if (provider.Equals(_referenceResolver.Provider))
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

        #region Parameter Validation

        private static ImmutableArray<T> ToImmutableArrayChecked<T>(ImmutableArray<T> items, string parameterName)
        {
            if (items.IsDefault)
            {
                throw new ArgumentNullException(parameterName);
            }

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null)
                {
                    throw new ArgumentNullException($"{parameterName}[{i}]");
                }
            }

            return items;
        }

        private static ImmutableArray<T> ToImmutableArrayChecked<T>(IEnumerable<T> items, string parameterName)
            where T : class
        {
            return AddRangeAndFreeChecked(ArrayBuilder<T>.GetInstance(), items, parameterName);
        }

        private static ImmutableArray<T> ConcatChecked<T>(ImmutableArray<T> existing, IEnumerable<T> items, string parameterName)
            where T : class
        {
            var builder = ArrayBuilder<T>.GetInstance();
            builder.AddRange(existing);
            return AddRangeAndFreeChecked(builder, items, parameterName);
        }

        private static ImmutableArray<T> AddRangeAndFreeChecked<T>(ArrayBuilder<T> builder, IEnumerable<T> items, string parameterName)
            where T : class
        {
            RequireNonNull(items, parameterName);

            foreach (var item in items)
            {
                if (item == null)
                {
                    builder.Free();
                    throw new ArgumentNullException($"{parameterName}[{builder.Count}]");
                }

                builder.Add(item);
            }

            return builder.ToImmutableAndFree();
        }

        private static IEnumerable<S> SelectChecked<T, S>(IEnumerable<T> items, string parameterName, Func<T, S> selector)
            where T : class
            where S : class
        {
            RequireNonNull(items, parameterName);
            return items.Select(item => (item != null) ? selector(item) : null);
        }

        private static void RequireNonNull<T>(IEnumerable<T> items, string parameterName)
        {
            if (items == null || items is ImmutableArray<T> && ((ImmutableArray<T>)items).IsDefault)
            {
                throw new ArgumentNullException(parameterName);
            }
        }

        #endregion
    }
}
