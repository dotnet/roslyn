// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed class AnalyzerAssemblyExtensions<TExtension> where TExtension : class
    {
        internal sealed class LoadedExtensions
        {
            public static readonly LoadedExtensions Empty = new LoadedExtensions(ImmutableArray<TExtension>.Empty, ImmutableArray<(string, Exception?)>.Empty);

            public readonly ImmutableArray<TExtension> Extensions;
            public readonly ImmutableArray<(string typeName, Exception?)> TypeLoadErrors;

            public LoadedExtensions(ImmutableArray<TExtension> extensions, ImmutableArray<(string, Exception?)> typeLoadErrors)
            {
                Extensions = extensions;
                TypeLoadErrors = typeLoadErrors;
            }
        }

        private readonly AnalyzerAssembly _analyzerAssembly;
        private readonly AnalyzerExtensionKind _kind;

        // hook for legacy event base error reporting
        private readonly Action<string, Exception?> _reportTypeLoadFailure;

        private LoadedExtensions? _lazyAllExtensions;
        private ImmutableDictionary<string, LoadedExtensions> _lazyExtensionsPerLanguage;

        internal AnalyzerAssemblyExtensions(AnalyzerAssembly analyzerAssembly, AnalyzerExtensionKind kind, Action<string, Exception?> reportTypeLoadFailure)
        {
            _analyzerAssembly = analyzerAssembly;
            _kind = kind;
            _lazyExtensionsPerLanguage = ImmutableDictionary<string, LoadedExtensions>.Empty;
            _reportTypeLoadFailure = reportTypeLoadFailure;
        }

        internal Exception? GetLoadException(string? language)
            => _analyzerAssembly.GetLoadException(language);

        internal LoadedExtensions GetExtensions(string? language)
        {
            if (language != null)
            {
                return ImmutableInterlocked.GetOrAdd(ref _lazyExtensionsPerLanguage, language, LoadExtensions);
            }

            if (_lazyAllExtensions == null)
            {
                Interlocked.CompareExchange(ref _lazyAllExtensions, LoadExtensions(language: null), null);
            }

            return _lazyAllExtensions;
        }

        private LoadedExtensions LoadExtensions(string? language)
        {
            if (!_analyzerAssembly.Load(language, _kind, out var assembly, out var typeMap))
            {
                return LoadedExtensions.Empty;
            }

            ImmutableHashSet<string>? languageTypeMap = null;

            if (language == null && typeMap.Count == 0 ||
                language != null && !typeMap.TryGetValue(language, out languageTypeMap))
            {
                return LoadedExtensions.Empty;
            }

            RoslynDebug.Assert(assembly != null);

            var extensions = ArrayBuilder<TExtension>.GetInstance();
            var typeLoadErrors = ArrayBuilder<(string, Exception?)>.GetInstance();

            if (language == null)
            {
                foreach (var (languageKey, languageSpecificTypeMap) in typeMap)
                {
                    if (languageKey == null)
                    {
                        continue;
                    }

                    InstantiateExtensions(extensions, typeLoadErrors, assembly, languageSpecificTypeMap);
                }
            }
            else
            {
                RoslynDebug.AssertNotNull(languageTypeMap);
                InstantiateExtensions(extensions, typeLoadErrors, assembly, languageTypeMap);
            }

            return new LoadedExtensions(extensions.ToImmutableAndFree(), typeLoadErrors.ToImmutableAndFree());
        }

        private void InstantiateExtensions(ArrayBuilder<TExtension> extensions, ArrayBuilder<(string, Exception?)> errors, Assembly analyzerAssembly, ImmutableHashSet<string> typeNames)
        {
            // Given the type names, get the actual System.Type and try to create an instance of the type through reflection.
            foreach (var typeName in typeNames)
            {
                void addError(Exception? exception)
                {
                    errors.Add((typeName, exception));
                    _reportTypeLoadFailure(typeName, exception);
                }

                Type? type;
                try
                {
                    type = analyzerAssembly.GetType(typeName, throwOnError: true, ignoreCase: false);
                }
                catch (Exception e)
                {
                    addError(e);
                    continue;
                }

                RoslynDebug.Assert(type != null);

                object? instance;
                try
                {
                    instance = Activator.CreateInstance(type);
                }
                catch (Exception e)
                {
                    addError(e);
                    continue;
                }

                if (instance is TExtension extension)
                {
                    extensions.Add(extension);
                }
                else
                {
                    addError(exception: null);
                }
            }
        }
    }
}
