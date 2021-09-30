// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

#pragma warning disable 8618
// ReSharper disable UnassignedGetOnlyAutoProperty

namespace Caravela.Compiler
{
    /// <summary>
    /// Context passed to a source transformer when <see cref="ISourceTransformer.Execute(TransformerContext)"/> is called.
    /// </summary>
    public sealed class TransformerContext
    {
#if !CARAVELA_COMPILER_INTERFACE
        private readonly DiagnosticBag _diagnostics;
        private readonly IAnalyzerAssemblyLoader _assemblyLoader;

        internal TransformerContext(
            Compilation compilation, ImmutableArray<object> plugins, AnalyzerConfigOptions globalOptions, IList<ResourceDescription> manifestResources,
            DiagnosticBag diagnostics, IAnalyzerAssemblyLoader assemblyLoader)
        {
            Compilation = compilation;
            Plugins = plugins;
            GlobalOptions = globalOptions;
            ManifestResources = manifestResources;
            _diagnostics = diagnostics;
            _assemblyLoader = assemblyLoader;
        }

        internal List<Action<DiagnosticRequest>> DiagnosticFilters { get; } = new();
#else
        private TransformerContext()
        {
        }
#endif

        /// <summary>
        /// Gets or sets the <see cref="Compilation"/>. Transformers typically replace the value of this property. 
        /// </summary>
        public Compilation Compilation { get; set; }

        /// <summary>
        /// Gets plugins that were registered by being marked with the <c>Caravela.CompilerPluginAttribute</c> attribute.
        /// </summary>
        public ImmutableArray<object> Plugins { get; }

        /// <summary>
        /// Allows access to global options provided by an analyzer config,
        /// which can in turn come from the csproj file.
        /// </summary>
        public AnalyzerConfigOptions GlobalOptions { get; }

        /// <summary>
        /// Gets the list of managed resources. Transformers can add, remove or change the list.
        ///
        /// To inspect existing resources, use extension methods from <see cref="ResourceDescriptionExtensions "/>.
        /// </summary>
        public IList<ResourceDescription> ManifestResources { get; }

        /// <summary>
        /// Adds a <see cref="Diagnostic"/> to the user's compilation.
        /// </summary>
        /// <param name="diagnostic">The diagnostic that should be added to the compilation</param>
        /// <remarks>
        /// The severity of the diagnostic may cause the compilation to fail, depending on the <see cref="Compilation"/> settings.
        /// </remarks>
        public void ReportDiagnostic(Diagnostic diagnostic)
        {
#if !CARAVELA_COMPILER_INTERFACE
            _diagnostics.Add(diagnostic);
#endif
        }

        /// <summary>
        /// Registers a delegate that can suppress a diagnostic.
        /// </summary>
        /// <param name="filter">A delegate that can suppress a diagnostic using <see cref="DiagnosticRequest.Suppress"/>.</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void RegisterDiagnosticFilter(Action<DiagnosticRequest> filter)
        {
#if CARAVELA_COMPILER_INTERFACE
            throw new InvalidOperationException("This operation works only inside Caravela.");
#else
            this.DiagnosticFilters.Add(filter);
#endif 
        }

        public Assembly LoadReferencedAssembly(IAssemblySymbol assemblySymbol)
        {
#if CARAVELA_COMPILER_INTERFACE
            throw new InvalidOperationException("This operation works only inside Caravela.");
#else
            if (Compilation.GetMetadataReference(assemblySymbol) is not { } reference)
                throw new ArgumentException("Could not retrieve MetadataReference for the given assembly symbol.", nameof(assemblySymbol));

            if (reference is not PortableExecutableReference peReference)
                throw new ArgumentException("The given assembly symbol does not correspond to a PE reference.", nameof(assemblySymbol));

            if (peReference.FilePath is not { } path)
                throw new ArgumentException("Could not access path for the given assembly symbol.", nameof(assemblySymbol));

            return _assemblyLoader.LoadFromPath(path);
#endif
        }
    }
}
