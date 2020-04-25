// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Represents analyzers stored in an analyzer assembly file.
    /// </summary>
    /// <remarks>
    /// Analyzer are read from the file, owned by the reference, and doesn't change 
    /// since the reference is accessed until the reference object is garbage collected.
    /// 
    /// If you need to manage the lifetime of the analyzer reference (and the file stream) explicitly use <see cref="AnalyzerImageReference"/>.
    /// </remarks>
    public sealed class AnalyzerFileReference : AnalyzerReference, IEquatable<AnalyzerReference>
    {
        private delegate bool AttributePredicate(PEModule module, CustomAttributeHandle attribute);
        private delegate IEnumerable<string> AttributeLanguagesFunc(PEModule module, CustomAttributeHandle attribute);

        private readonly AnalyzerAssemblyExtensions<DiagnosticAnalyzer> _diagnosticAnalyzers;
        private readonly AnalyzerAssemblyExtensions<ISourceGenerator> _generators;
        private readonly AnalyzerAssembly _analyzerAssembly;

        private string? _lazyDisplay;
        private object? _lazyIdentity;

        // obsolete
        public event EventHandler<AnalyzerLoadFailureEventArgs>? AnalyzerLoadFailed;

        /// <summary>
        /// Creates an AnalyzerFileReference with the given <paramref name="fullPath"/> and <paramref name="assemblyLoader"/>.
        /// </summary>
        /// <param name="fullPath">Full path of the analyzer assembly.</param>
        /// <param name="assemblyLoader">Loader for obtaining the <see cref="Assembly"/> from the <paramref name="fullPath"/></param>
        public AnalyzerFileReference(string fullPath, IAnalyzerAssemblyLoader assemblyLoader)
        {
            CompilerPathUtilities.RequireAbsolutePath(fullPath, nameof(fullPath));
            if (assemblyLoader == null)
            {
                throw new ArgumentNullException(nameof(assemblyLoader));
            }

            _analyzerAssembly = new AnalyzerAssembly(fullPath, assemblyLoader, ReportAssemblyLoadFailure);
            _diagnosticAnalyzers = new AnalyzerAssemblyExtensions<DiagnosticAnalyzer>(_analyzerAssembly, AnalyzerExtensionKind.DiagnosticAnalyzer, ReportExtensionTypeLoadFailure);
            _generators = new AnalyzerAssemblyExtensions<ISourceGenerator>(_analyzerAssembly, AnalyzerExtensionKind.Generator, ReportExtensionTypeLoadFailure);

            // Note this analyzer full path as a dependency location, so that the analyzer loader
            // can correctly load analyzer dependencies.
            assemblyLoader.AddDependencyLocation(fullPath);
        }

        private void ReportExtensionTypeLoadFailure(string typeName, Exception? exception)
            => AnalyzerLoadFailed?.Invoke(this, AnalyzerLoadFailureEventArgs.Create(exception, typeName));

        private void ReportAssemblyLoadFailure(Exception exception)
            => AnalyzerLoadFailed?.Invoke(this, AnalyzerLoadFailureEventArgs.Create(exception, typeName: null));

        public override string FullPath
            => _analyzerAssembly.FullPath;

        public IAnalyzerAssemblyLoader AssemblyLoader
            => _analyzerAssembly.Loader;

        public override bool Equals(object? obj)
            => Equals(obj as AnalyzerFileReference);

        public bool Equals(AnalyzerFileReference? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return other is object &&
                ReferenceEquals(_assemblyLoader, other._assemblyLoader) &&
                FullPath == other.FullPath;
        }

        // legacy, for backwards compat:
        public bool Equals(AnalyzerReference? other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (other is null)
            {
                return false;
            }

            if (other is AnalyzerFileReference fileReference)
            {
                return Equals(fileReference);
            }

            return FullPath == other.FullPath;
        }

        public override int GetHashCode()
            => Hash.Combine(RuntimeHelpers.GetHashCode(AssemblyLoader), FullPath.GetHashCode());

        public override string Display
        {
            get
            {
                if (_lazyDisplay == null)
                {
                    InitializeDisplayAndId();
                }

                // Use MemberNotNull when available https://github.com/dotnet/roslyn/issues/41964
                return _lazyDisplay!;
            }
        }

        public override object Id
        {
            get
            {
                if (_lazyIdentity == null)
                {
                    InitializeDisplayAndId();
                }

                // Use MemberNotNull when available https://github.com/dotnet/roslyn/issues/41964
                return _lazyIdentity!;
            }
        }

        private void InitializeDisplayAndId()
        {
            try
            {
                // AssemblyName.GetAssemblyName(path) is not available on CoreCLR.
                // Use our metadata reader to do the equivalent thing.
                using var reader = new PEReader(FileUtilities.OpenRead(FullPath));

                var metadataReader = reader.GetMetadataReader();
                var assemblyIdentity = metadataReader.ReadAssemblyIdentityOrThrow();
                _lazyDisplay = assemblyIdentity.Name;
                _lazyIdentity = assemblyIdentity;
            }
            catch
            {
                _lazyDisplay = FileNameUtilities.GetFileName(FullPath, includeExtension: false);
                _lazyIdentity = _lazyDisplay;
            }
        }

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzersForAllLanguages()
            => _diagnosticAnalyzers.GetExtensions(language: null).Extensions;

        public override ImmutableArray<DiagnosticAnalyzer> GetAnalyzers(string language)
            => _diagnosticAnalyzers.GetExtensions(ValidateLanguage(language)).Extensions;

        public override ImmutableArray<ISourceGenerator> GetGenerators()
            => _generators.GetExtensions(language: null).Extensions;

        public override ImmutableArray<AnalyzerLoadFailure> GetLoadErrorsForAllLanguages()
            => GetLoadErrorsImpl(language: null);

        public override ImmutableArray<AnalyzerLoadFailure> GetLoadErrors(string language)
            => GetLoadErrorsImpl(ValidateLanguage(language));

        private ImmutableArray<AnalyzerLoadFailure> GetLoadErrorsImpl(string? language)
        {
            var failures = ArrayBuilder<AnalyzerLoadFailure>.GetInstance();

            void addTypeLoadFailures(ImmutableArray<(string, Exception?)> errors)
            {
                foreach (var (typeName, exception) in errors)
                {
                    var errorCode = (exception != null) ? AnalyzerLoadFailure.ErrorCode.TypeLoadFailure : AnalyzerLoadFailure.ErrorCode.InvalidImplementation;
                    failures.Add(new AnalyzerLoadFailure(errorCode, typeName, exception));
                }
            }

            var assemblyLoadException = _analyzerAssembly.GetLoadException(language);
            if (assemblyLoadException != null)
            {
                failures.Add(new AnalyzerLoadFailure(AnalyzerLoadFailure.ErrorCode.AssemblyLoadFailure, typeName: null, assemblyLoadException));
            }

            // add errors for all extensions:
            addTypeLoadFailures(_diagnosticAnalyzers.GetExtensions(language).TypeLoadErrors);
            addTypeLoadFailures(_generators.GetExtensions(language).TypeLoadErrors);

            return failures.ToImmutableAndFree();
        }

        /// <summary>
        /// Used by the command line compiler to convert and filter load error errors.
        /// </summary>
        internal void AddDiagnostics(IList<DiagnosticInfo> diagnostics, string language, CommonMessageProvider messageProvider, CompilationOptions options)
        {
            void addDiagnostic(DiagnosticInfo diagnostic)
            {
                // Filter this diagnostic based on the compilation options so that /nowarn and /warnaserror etc. take effect.
                var filteredDiagnostic = messageProvider.FilterDiagnosticInfo(diagnostic, options);
                if (filteredDiagnostic != null)
                {
                    diagnostics.Add(filteredDiagnostic);
                }
            }

            void addTypeLoadDiagnostics(ImmutableArray<(string, Exception?)> errors)
            {
                foreach (var (typeName, exception) in errors)
                {
                    if (exception == null)
                    {
                        // TODO: change error id
                        // An extension is marked with an attribute but does not implement the required type (DiagnosticAnalyzer, ISourceGenerator).
                        addDiagnostic(new DiagnosticInfo(messageProvider, messageProvider.WRN_NoAnalyzerInAssembly, FullPath));
                    }
                    else
                    {
                        addDiagnostic(new DiagnosticInfo(messageProvider, messageProvider.WRN_AnalyzerCannotBeCreated, typeName, FullPath, exception.Message));
                    }
                }
            }

            var assemblyLoadException = _analyzerAssembly.GetLoadException(language);
            if (assemblyLoadException != null)
            {
                addDiagnostic(new DiagnosticInfo(messageProvider, messageProvider.WRN_UnableToLoadAnalyzer, FullPath, assemblyLoadException.Message));
            }

            // add diagnostics for all extensions:
            addTypeLoadDiagnostics(_diagnosticAnalyzers.GetExtensions(language).TypeLoadErrors);
            addTypeLoadDiagnostics(_generators.GetExtensions(language).TypeLoadErrors);
        }

        private static string ValidateLanguage(string language)
        {
            if (language == null)
            {
                throw new ArgumentNullException(nameof(language));
            }

            if (language.IsEmpty())
            {
                throw new ArgumentException(nameof(language));
            }

            return language;
        }
    }
}
