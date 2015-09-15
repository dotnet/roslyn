// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace Microsoft.CodeAnalysis.Scripting
{
    /// <summary>
    /// Options for creating and running scripts.
    /// </summary>
    public sealed class ScriptOptions
    {
        public static readonly ScriptOptions Default = new ScriptOptions(
            path: "", 
            references: ImmutableArray<MetadataReference>.Empty,
            namespaces: ImmutableArray<string>.Empty,
            metadataResolver: RuntimeMetadataReferenceResolver.Default,
            sourceResolver: SourceFileResolver.Default,
            isInteractive: true);

        /// <summary>
        /// An array of <see cref="MetadataReference"/>s to be added to the script.
        /// </summary>
        /// <remarks>
        /// The array may contain both resolved and unresolved references (<see cref="UnresolvedMetadataReference"/>).
        /// Unresolved references are resolved when the script is about to be executed 
        /// (<see cref="Script.RunAsync(object, CancellationToken)"/>.
        /// Any resolution errors are reported at that point through <see cref="CompilationErrorException"/>.
        /// </remarks>
        public ImmutableArray<MetadataReference> MetadataReferences { get; private set; }

        /// <summary>
        /// <see cref="MetadataReferenceResolver"/> to be used to resolve missing dependencies, unresolved metadata references and #r directives.
        /// </summary>
        public MetadataReferenceResolver MetadataResolver { get; private set; }

        /// <summary>
        /// <see cref="SourceReferenceResolver"/> to be used to resolve source of scripts referenced via #load directive.
        /// </summary>
        public SourceReferenceResolver SourceResolver { get; private set; }

        /// <summary>
        /// The namespaces automatically imported by the script.
        /// </summary>
        public ImmutableArray<string> Namespaces { get; private set; }

        /// <summary>
        /// The path to the script source if it originated from a file, empty otherwise.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// True if the script is interactive. 
        /// Interactive scripts may contain a final expression whose value is returned when the script is run.
        /// </summary>
        public bool IsInteractive { get; private set; }

        private ScriptOptions(
            string path,
            ImmutableArray<MetadataReference> references,
            ImmutableArray<string> namespaces,
            MetadataReferenceResolver metadataResolver,
            SourceReferenceResolver sourceResolver,
            bool isInteractive)
        {
            Debug.Assert(path != null);
            Debug.Assert(!references.IsDefault);
            Debug.Assert(!namespaces.IsDefault);
            Debug.Assert(metadataResolver != null);
            Debug.Assert(sourceResolver != null);

            Path = path;
            MetadataReferences = references;
            Namespaces = namespaces;
            MetadataResolver = metadataResolver;
            SourceResolver = sourceResolver;
            IsInteractive = isInteractive;
        }

        private ScriptOptions(ScriptOptions other) 
            : this(path: other.Path,
                   references: other.MetadataReferences,
                   namespaces: other.Namespaces,
                   metadataResolver: other.MetadataResolver,
                   sourceResolver: other.SourceResolver,
                   isInteractive: other.IsInteractive)
        {
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the <see cref="Path"/> changed.
        /// </summary>
        public ScriptOptions WithPath(string path) =>
            (Path == path) ? this : new ScriptOptions(this) { Path = path ?? "" };

        private static MetadataReference CreateUnresolvedReference(string reference) =>
            new UnresolvedMetadataReference(reference, MetadataReferenceProperties.Assembly);

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the references changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is null or contains a null reference.</exception>
        public ScriptOptions WithReferences(ImmutableArray<MetadataReference> references) =>
            MetadataReferences.Equals(references) ? this : new ScriptOptions(this) { MetadataReferences = CheckImmutableArray(references, nameof(references)) };

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
            WithReferences(ConcatChecked(MetadataReferences, references, nameof(references)));

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
            WithReferences(SelectChecked(references, nameof(references), CreateUnresolvedReference));

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
            AddReferences(SelectChecked(references, nameof(references), CreateUnresolvedReference));

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with references added.
        /// </summary>
        public ScriptOptions AddReferences(params string[] references) => 
            AddReferences((IEnumerable<string>)references);

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with <see cref="MetadataResolver"/> set to the default metadata resolver for the current platform.
        /// </summary>
        /// <param name="searchPaths">Directories to be used by the default resolver when resolving assembly file names.</param>
        /// <remarks>
        /// The default resolver looks up references in specified <paramref name="searchPaths"/>, in NuGet packages and in Global Assembly Cache (if available on the current platform).
        /// </remarks>
        public ScriptOptions WithDefaultMetadataResolution(ImmutableArray<string> searchPaths)
        {
            var resolver = new RuntimeMetadataReferenceResolver(
                ToImmutableArrayChecked(searchPaths, nameof(searchPaths)),
                baseDirectory: null);

            return new ScriptOptions(this) { MetadataResolver = resolver };
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with <see cref="MetadataResolver"/> set to the default metadata resolver for the current platform.
        /// </summary>
        /// <param name="searchPaths">Directories to be used by the default resolver when resolving assembly file names.</param>
        /// <remarks>
        /// The default resolver looks up references in specified <paramref name="searchPaths"/>, in NuGet packages and in Global Assembly Cache (if available on the current platform).
        /// </remarks>
        public ScriptOptions WithDefaultMetadataResolution(IEnumerable<string> searchPaths) =>
            WithDefaultMetadataResolution(searchPaths.AsImmutableOrEmpty());

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with <see cref="MetadataResolver"/> set to the default metadata resolver for the current platform.
        /// </summary>
        /// <param name="searchPaths">Directories to be used by the default resolver when resolving assembly file names.</param>
        /// <remarks>
        /// The default resolver looks up references in specified <paramref name="searchPaths"/>, in NuGet packages and in Global Assembly Cache (if available on the current platform).
        /// </remarks>
        public ScriptOptions WithDefaultMetadataResolution(params string[] searchPaths) =>
            WithDefaultMetadataResolution(searchPaths.AsImmutableOrEmpty());

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with specified <see cref="MetadataResolver"/>.
        /// </summary>
        public ScriptOptions WithCustomMetadataResolution(MetadataReferenceResolver resolver) =>
            MetadataResolver == resolver ? this : new ScriptOptions(this) { MetadataResolver = resolver };

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with <see cref="SourceResolver"/> set to the default source resolver for the current platform.
        /// </summary>
        /// <param name="searchPaths">Directories to be used by the default resolver when resolving script file names.</param>
        /// <remarks>
        /// The default resolver looks up scripts in specified <paramref name="searchPaths"/> and in NuGet packages.
        /// </remarks>
        public ScriptOptions WithDefaultSourceResolution(ImmutableArray<string> searchPaths)
        {
            var resolver = new SourceFileResolver(
                ToImmutableArrayChecked(searchPaths, nameof(searchPaths)),
                baseDirectory: null);

            return new ScriptOptions(this) { SourceResolver = resolver };
        }

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with <see cref="SourceResolver"/> set to the default source resolver for the current platform.
        /// </summary>
        /// <param name="searchPaths">Directories to be used by the default resolver when resolving script file names.</param>
        /// <remarks>
        /// The default resolver looks up scripts in specified <paramref name="searchPaths"/> and in NuGet packages.
        /// </remarks>
        public ScriptOptions WithDefaultSourceResolution(IEnumerable<string> searchPaths) =>
           WithDefaultSourceResolution(searchPaths.AsImmutableOrEmpty());

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with <see cref="SourceResolver"/> set to the default source resolver for the current platform.
        /// </summary>
        /// <param name="searchPaths">Directories to be used by the default resolver when resolving script file names.</param>
        /// <remarks>
        /// The default resolver looks up scripts in specified <paramref name="searchPaths"/> and in NuGet packages.
        /// </remarks>
        public ScriptOptions WithDefaultSourceResolution(params string[] searchPaths) =>
           WithDefaultSourceResolution(searchPaths.AsImmutableOrEmpty());

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with specified <see cref="SourceResolver"/>.
        /// </summary>
        public ScriptOptions WithCustomSourceResolution(SourceReferenceResolver resolver) =>
            SourceResolver == resolver ? this : new ScriptOptions(this) { SourceResolver = resolver };

        /// <summary>
        /// Creates a new <see cref="ScriptOptions"/> with the namespaces changed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="namespaces"/> is null or contains a null reference.</exception>
        public ScriptOptions WithNamespaces(ImmutableArray<string> namespaces) =>
            Namespaces.Equals(namespaces) ? this : new ScriptOptions(this) { Namespaces = CheckImmutableArray(namespaces, nameof(namespaces)) };

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
        /// Create a new <see cref="ScriptOptions"/> with the interactive state specified.
        /// Interactive scripts may contain a final expression whose value is returned when the script is run.
        /// </summary>
        public ScriptOptions WithIsInteractive(bool isInteractive) =>
            IsInteractive == isInteractive ? this : new ScriptOptions(this) { IsInteractive = isInteractive };

        #region Parameter Validation

        private static ImmutableArray<T> CheckImmutableArray<T>(ImmutableArray<T> items, string parameterName)
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
