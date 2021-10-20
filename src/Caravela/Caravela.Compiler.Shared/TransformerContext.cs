// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

#pragma warning disable 8618
// ReSharper disable UnassignedGetOnlyAutoProperty

namespace Caravela.Compiler
{
    /// <summary>
    /// Context passed to a source transformer when <see cref="ISourceTransformer.Execute(TransformerContext)"/> is called.
    /// The implementation can modify the compilation using the methods <see cref="AddSyntaxTrees(Microsoft.CodeAnalysis.SyntaxTree[])"/>, <see cref="ReplaceSyntaxTree"/> or
    /// <see cref="AddResources(Microsoft.CodeAnalysis.ResourceDescription[])"/>. It can report a diagnostic using <see cref="ReportDiagnostic"/> or suppress diagnostics using <see cref="RegisterDiagnosticFilter"/>.
    /// </summary>
    public sealed class TransformerContext
    {
#if !CARAVELA_COMPILER_INTERFACE
        private readonly DiagnosticBag _diagnostics;
        private readonly IAnalyzerAssemblyLoader _assemblyLoader;

        internal List<SyntaxTreeTransformation> TransformedTrees { get; } = new();
        internal List<ResourceDescription> AddedResources { get; } = new();
        internal List<DiagnosticFilter> DiagnosticFilters { get; } = new();

        internal TransformerContext(
            Compilation compilation, ImmutableArray<object> plugins, AnalyzerConfigOptions globalOptions, ImmutableArray<ResourceDescription> manifestResources,
            DiagnosticBag diagnostics, IAnalyzerAssemblyLoader assemblyLoader)
        {
            Compilation = compilation;
            Plugins = plugins;
            GlobalOptions = globalOptions;
            Resources = manifestResources;
            _diagnostics = diagnostics;
            _assemblyLoader = assemblyLoader;
        }
#else
        private TransformerContext()
        {
        }
#endif

        public void ReplaceSyntaxTree(SyntaxTree oldTree, SyntaxTree newTree)
        {
#if !CARAVELA_COMPILER_INTERFACE            
            if (!this.Compilation.ContainsSyntaxTree(oldTree))
            {
                throw new InvalidOperationException("The original compilation does not contain this syntax tree.");
            }
            
            if ( oldTree == newTree )
            {
                return;
            }

            TrackTreeReplacement(oldTree, newTree);

            this.TransformedTrees.Add(new SyntaxTreeTransformation(newTree, oldTree));
#endif            
        }

#if !CARAVELA_COMPILER_INTERFACE
        private static void TrackTreeReplacement(SyntaxTree oldTree, SyntaxTree newTree)
        {
            SyntaxTreeHistory.Update(oldTree, newTree);
        }
#endif

        public void AddSyntaxTreeTransformations(IEnumerable<SyntaxTreeTransformation> transformations)
        {
#if !CARAVELA_COMPILER_INTERFACE
            foreach (var transformation in transformations)
            {
                if (transformation.NewTree == transformation.OldTree)
                {
                    continue;
                }

                if (transformation.OldTree != null)
                {
                    TrackTreeReplacement(transformation.OldTree, transformation.NewTree);
                }
                this.TransformedTrees.Add(transformation);
            }
#endif
        }

        public void AddSyntaxTrees(params SyntaxTree[] syntaxTrees)
        {
#if !CARAVELA_COMPILER_INTERFACE
            this.TransformedTrees.AddRange(syntaxTrees.Select(t => new SyntaxTreeTransformation(t, null)));
#endif
        }

        public void AddSyntaxTrees(IEnumerable<SyntaxTree> syntaxTrees)
        {
#if !CARAVELA_COMPILER_INTERFACE
            this.TransformedTrees.AddRange(syntaxTrees.Select(t => new SyntaxTreeTransformation(t, null)));
#endif
        }

        /// <summary>
        /// Gets or sets the <see cref="Compilation"/>. Transformers typically replace the value of this property. 
        /// </summary>
        public Compilation Compilation { get; }

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
        /// Gets the list of managed resources. 
        ///
        /// To inspect existing resources, use extension methods from <see cref="ResourceDescriptionExtensions "/>.
        /// </summary>
        public ImmutableArray<ResourceDescription> Resources { get; }

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

        public void AddResources(params ResourceDescription[] resources)
        {
#if !CARAVELA_COMPILER_INTERFACE
            this.AddedResources.AddRange(resources);
#endif
        }

        public void AddResources(IEnumerable<ResourceDescription> resources)
        {
#if !CARAVELA_COMPILER_INTERFACE
            this.AddedResources.AddRange(resources);
#endif
        }

        /// <summary>
        /// Registers a delegate that can suppress a diagnostic.
        /// </summary>
        /// <param name="filter">A delegate that can suppress a diagnostic using <see cref="DiagnosticFilteringRequest.Suppress"/>.</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void RegisterDiagnosticFilter(SuppressionDescriptor descriptor, Action<DiagnosticFilteringRequest> filter)
        {
#if !CARAVELA_COMPILER_INTERFACE
            this.DiagnosticFilters.Add(new DiagnosticFilter(descriptor, filter));
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
