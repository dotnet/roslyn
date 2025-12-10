// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal abstract class SyntaxTreeKey
    {
        public abstract string FilePath { get; }
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

            public override string FilePath
                => _tree.FilePath;

            public override ParseOptions Options
                => _tree.Options;

            public override SourceText GetText(CancellationToken cancellationToken = default)
                => _tree.GetText(cancellationToken);
        }
    }

    internal static class DeterministicKey
    {
        public static string GetDeterministicKey(
            CompilationOptions compilationOptions,
            ImmutableArray<SyntaxTree> syntaxTrees,
            ImmutableArray<MetadataReference> references,
            ImmutableArray<byte> publicKey = default,
            ImmutableArray<AdditionalText> additionalTexts = default,
            ImmutableArray<DiagnosticAnalyzer> analyzers = default,
            ImmutableArray<ISourceGenerator> generators = default,
            ImmutableArray<KeyValuePair<string, string>> pathMap = default,
            EmitOptions? emitOptions = null,
            SourceText? sourceLinkText = null,
            string? ruleSetFilePath = null,
            ImmutableArray<ResourceDescription> resources = default,
            DeterministicKeyOptions options = DeterministicKeyOptions.Default,
            CancellationToken cancellationToken = default)
        {
            var keyBuilder = compilationOptions.CreateDeterministicKeyBuilder();
            return keyBuilder.GetKey(
                compilationOptions,
                syntaxTrees.SelectAsArray(static t => SyntaxTreeKey.Create(t)),
                references,
                publicKey,
                additionalTexts,
                analyzers,
                generators,
                pathMap,
                emitOptions,
                sourceLinkText,
                ruleSetFilePath,
                resources,
                options,
                cancellationToken);
        }
    }
}
