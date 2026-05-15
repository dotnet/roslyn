// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Holds input nodes that are shared between generators and always exist
    /// </summary>
    internal static class SharedInputNodes
    {
        public static readonly InputNode<Compilation> Compilation = new InputNode<Compilation>(b => ImmutableArray.Create(GetCompilationOrThrow(b, nameof(IncrementalGeneratorInitializationContext.CompilationProvider))));

        public static readonly InputNode<CompilationOptions> CompilationOptions = new InputNode<CompilationOptions>(b => ImmutableArray.Create(b.InitialCompilationOptions), ReferenceEqualityComparer.Instance);

        public static readonly InputNode<ParseOptions> ParseOptions = new InputNode<ParseOptions>(b => ImmutableArray.Create(b.DriverState.ParseOptions));

        public static readonly InputNode<AdditionalText> AdditionalTexts = new InputNode<AdditionalText>(b => b.DriverState.AdditionalTexts);

        public static readonly InputNode<SyntaxTree> SyntaxTrees = new InputNode<SyntaxTree>(b => GetCompilationOrThrow(b, nameof(IncrementalGeneratorInitializationContext.SyntaxProvider)).SyntaxTrees.ToImmutableArray());

        public static readonly InputNode<AnalyzerConfigOptionsProvider> AnalyzerConfigOptions = new InputNode<AnalyzerConfigOptionsProvider>(b => ImmutableArray.Create(b.DriverState.OptionsProvider));

        public static readonly InputNode<MetadataReference> MetadataReferences = new InputNode<MetadataReference>(b => b.InitialMetadataReferences);

        private static Compilation GetCompilationOrThrow(DriverStateTable.Builder b, string providerName)
        {
            if (!b.IsCompilationAvailable)
            {
                // The full compilation (including syntax trees) is not available during the pre-compilation phase;
                // CompilationProvider and SyntaxProvider must wait for the standard phase. Note that
                // CompilationOptions and MetadataReferences ARE available pre-compilation, since they
                // are unaffected by source generation.
                throw new UserFunctionException(new InvalidOperationException(
                    string.Format(CodeAnalysisResources.CompilationNotAvailableInPreCompilationPhase, providerName)));
            }
            return b.Compilation;
        }
    }
}
