// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    public abstract class SyntaxTreeKey
    {
        public abstract string? FilePath { get; }
        public abstract ParseOptions Options { get; }

        protected SyntaxTreeKey()
        {
        }

        public abstract SourceText GetText(CancellationToken cancellationToken = default);

        public static SyntaxTreeKey Create(SyntaxTree tree)
            => new DefaultSyntaxTreeKey(tree);

        private sealed class DefaultSyntaxTreeKey : SyntaxTreeKey
        {
            private readonly SyntaxTree _tree;

            public DefaultSyntaxTreeKey(SyntaxTree tree)
            {
                _tree = tree;
            }

            public override string? FilePath
                => _tree.FilePath;

            public override ParseOptions Options
                => _tree.Options;

            public override SourceText GetText(CancellationToken cancellationToken = default)
                => _tree.GetText(cancellationToken);
        }
    }

    public static class DeterministicKey
    {
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static string GetDeterministicKey(
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
            CompilationOptions compilationOptions,
            ImmutableArray<SyntaxTree> syntaxTrees,
            ImmutableArray<MetadataReference> references,
            ImmutableArray<AdditionalText> additionalTexts = default,
            ImmutableArray<DiagnosticAnalyzer> analyzers = default,
            ImmutableArray<ISourceGenerator> generators = default,
            EmitOptions? emitOptions = null,
            DeterministicKeyOptions options = DeterministicKeyOptions.Default,
            CancellationToken cancellationToken = default)
        {
            return GetDeterministicKey(
                compilationOptions,
                syntaxTrees.SelectAsArray(static t => SyntaxTreeKey.Create(t)),
                references,
                additionalTexts,
                analyzers,
                generators,
                emitOptions,
                options,
                cancellationToken);
        }

#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static string GetDeterministicKey(
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
            CompilationOptions compilationOptions,
            ImmutableArray<SyntaxTreeKey> syntaxTreeKeys,
            ImmutableArray<MetadataReference> references,
            ImmutableArray<AdditionalText> additionalTexts = default,
            ImmutableArray<DiagnosticAnalyzer> analyzers = default,
            ImmutableArray<ISourceGenerator> generators = default,
            EmitOptions? emitOptions = null,
            DeterministicKeyOptions options = DeterministicKeyOptions.Default,
            CancellationToken cancellationToken = default)
        {
            var keyBuilder = compilationOptions.CreateDeterministicKeyBuilder();
            return keyBuilder.GetKey(
                compilationOptions,
                syntaxTreeKeys,
                references,
                additionalTexts.NullToEmpty(),
                analyzers.NullToEmpty(),
                generators.NullToEmpty(),
                emitOptions,
                options,
                cancellationToken);
        }
    }
}
