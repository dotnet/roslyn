// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

#if !METALAMA_COMPILER_INTERFACE
using System.Linq;
#endif

#pragma warning disable CS8618
// ReSharper disable UnassignedGetOnlyAutoProperty

namespace Metalama.Compiler;

/// <summary>
/// Context passed to a source transformer when <see cref="ISourceTransformer.Execute(TransformerContext)"/> is called.
/// The implementation can modify the compilation using the methods <see cref="AddSyntaxTrees(Microsoft.CodeAnalysis.SyntaxTree[])"/>, <see cref="ReplaceSyntaxTree"/> or
/// <see cref="AddResources(ManagedResource[])"/>. It can report a diagnostic using <see cref="ReportDiagnostic"/> or suppress diagnostics using <see cref="RegisterDiagnosticFilter"/>.
/// </summary>
// ReSharper disable once ClassCannotBeInstantiated
public sealed class TransformerContext
{
#if !METALAMA_COMPILER_INTERFACE
    private readonly DiagnosticBag _diagnostics;
    private readonly IAnalyzerAssemblyLoader _assemblyLoader;

    internal List<SyntaxTreeTransformation> TransformedTrees { get; } = new();
    internal List<ManagedResource> AddedResources { get; } = new();
    internal List<DiagnosticFilter> DiagnosticFilters { get; } = new();

    internal TransformerContext(
        Compilation compilation,
        AnalyzerConfigOptionsProvider analyzerConfigOptionsProvider,
        TransformerOptions options,
        ImmutableArray<ManagedResource> manifestResources,
        DiagnosticBag diagnostics,
        IAnalyzerAssemblyLoader assemblyLoader)
    {
        Compilation = compilation;
        Options = options;
        AnalyzerConfigOptionsProvider = analyzerConfigOptionsProvider;
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
#if !METALAMA_COMPILER_INTERFACE
        if (!Compilation.ContainsSyntaxTree(oldTree))
        {
            throw new InvalidOperationException("The original compilation does not contain this syntax tree.");
        }

        if (oldTree == newTree)
        {
            return;
        }

        TrackTreeReplacement(oldTree, newTree);

        TransformedTrees.Add(SyntaxTreeTransformation.ReplaceTree(oldTree, newTree));
#endif
    }

#if !METALAMA_COMPILER_INTERFACE
    private static void TrackTreeReplacement(SyntaxTree oldTree, SyntaxTree newTree)
    {
        SyntaxTreeHistory.Update(oldTree, newTree);
    }
#endif

    public void AddSyntaxTreeTransformations(params SyntaxTreeTransformation[] transformations)
    {
        AddSyntaxTreeTransformations((IEnumerable<SyntaxTreeTransformation>)transformations);
    }

    public void AddSyntaxTreeTransformations(IEnumerable<SyntaxTreeTransformation> transformations)
    {
#if !METALAMA_COMPILER_INTERFACE
        foreach (var transformation in transformations)
        {
            if (transformation.NewTree == transformation.OldTree)
            {
                continue;
            }

            if (transformation.OldTree != null && transformation.NewTree != null)
            {
                if (!Compilation.ContainsSyntaxTree(transformation.OldTree))
                {
                    throw new InvalidOperationException(
                        $"The original compilation does not contain the syntax tree '{transformation.OldTree.FilePath}'.");
                }

                TrackTreeReplacement(transformation.OldTree, transformation.NewTree);
            }

            TransformedTrees.Add(transformation);
        }
#endif
    }

    public void AddSyntaxTrees(params SyntaxTree[] syntaxTrees)
    {
#if !METALAMA_COMPILER_INTERFACE
        TransformedTrees.AddRange(syntaxTrees.Select(SyntaxTreeTransformation.AddTree));
#endif
    }

    public void AddSyntaxTrees(IEnumerable<SyntaxTree> syntaxTrees)
    {
#if !METALAMA_COMPILER_INTERFACE
        TransformedTrees.AddRange(syntaxTrees.Select(SyntaxTreeTransformation.AddTree));
#endif
    }

    /// <summary>
    /// Gets the original <see cref="Compilation"/>.
    /// Transformers typically modify the compilation by using methods on this <see cref="TransformerContext" />,
    /// though such modifications are not reflection on this property.
    /// </summary>
    public Compilation Compilation { get; }

    /// <summary>
    /// Gets options of the current <see cref="TransformerContext"/>.
    /// </summary>
    public TransformerOptions Options { get; }

    /// <summary>
    /// Gets the <see cref="AnalyzerConfigOptionsProvider"/>, which allows to access <c>.editorconfig</c> options.
    /// </summary>
    public AnalyzerConfigOptionsProvider AnalyzerConfigOptionsProvider { get; }

    /// <summary>
    /// Gets the list of managed resources. 
    /// </summary>
    public ImmutableArray<ManagedResource> Resources { get; }

    /// <summary>
    /// Adds a <see cref="Diagnostic"/> to the user's compilation.
    /// </summary>
    /// <param name="diagnostic">The diagnostic that should be added to the compilation</param>
    /// <remarks>
    /// The severity of the diagnostic may cause the compilation to fail, depending on the <see cref="Compilation"/> settings.
    /// </remarks>
    public void ReportDiagnostic(Diagnostic diagnostic)
    {
#if !METALAMA_COMPILER_INTERFACE
        _diagnostics.Add(diagnostic);
#endif
    }

    public void AddResources(params ManagedResource[] resources)
    {
#if !METALAMA_COMPILER_INTERFACE
        AddedResources.AddRange(resources);
#endif
    }

    public void AddResources(IEnumerable<ManagedResource> resources)
    {
#if !METALAMA_COMPILER_INTERFACE
        AddedResources.AddRange(resources);
#endif
    }

    /// <summary>
    /// Registers a delegate that can suppress a diagnostic.
    /// </summary>
    /// <param name="filter">A delegate that can suppress a diagnostic using <see cref="DiagnosticFilteringRequest.Suppress"/>.</param>
    /// <exception cref="InvalidOperationException"></exception>
    public void RegisterDiagnosticFilter(SuppressionDescriptor descriptor, Action<DiagnosticFilteringRequest> filter)
    {
#if !METALAMA_COMPILER_INTERFACE
        DiagnosticFilters.Add(new DiagnosticFilter(descriptor, filter));
#endif
    }

    public Assembly LoadReferencedAssembly(IAssemblySymbol assemblySymbol)
    {
#if METALAMA_COMPILER_INTERFACE
        throw new InvalidOperationException("This operation works only inside Metalama.");
#else
        // ReSharper disable LocalizableElement
        if (Compilation.GetMetadataReference(assemblySymbol) is not { } reference)
        {
            throw new ArgumentException("Could not retrieve MetadataReference for the given assembly symbol.",
                nameof(assemblySymbol));
        }

        if (reference is not PortableExecutableReference peReference)
        {
            throw new ArgumentException("The given assembly symbol does not correspond to a PE reference.",
                nameof(assemblySymbol));
        }

        if (peReference.FilePath is not { } path)
        {
            throw new ArgumentException("Could not access path for the given assembly symbol.", nameof(assemblySymbol));
        }
        // ReSharper restore LocalizableElement

        return _assemblyLoader.LoadFromPath(path);
#endif
    }
}

/// <summary>
/// Options of a <see cref="ISourceTransformer"/>, exposed on <see cref="TransformerContext.Options"/>.
/// </summary>
public sealed class TransformerOptions
{
    /// <summary>
    /// Gets or sets a value indicating that transformers should annotate
    /// the code with code coverage annotations from <see cref="MetalamaCompilerAnnotations"/>.
    /// </summary>
    public bool RequiresCodeCoverageAnnotations { get; }

    internal TransformerOptions(bool requiresCodeCoverageAnnotations)
    {
        RequiresCodeCoverageAnnotations = requiresCodeCoverageAnnotations;
    }

    public static TransformerOptions Default { get; } = new(requiresCodeCoverageAnnotations: false);
}
