// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Adapts an ISourceGenerator to an incremental generator that
    /// by providing an execution environment that matches the old one
    /// </summary>
    internal sealed class SourceGeneratorAdaptor : IIncrementalGenerator
    {
        /// <summary>
        /// A dummy extension that is used to indicate this adaptor was created outside of the driver.
        /// </summary>
        public const string DummySourceExtension = ".dummy";

        private readonly string _sourceExtension;

        internal ISourceGenerator SourceGenerator { get; }

        public SourceGeneratorAdaptor(ISourceGenerator generator, string sourceExtension)
        {
            SourceGenerator = generator;
            _sourceExtension = sourceExtension;
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // We don't currently have any APIs that accept IIncrementalGenerator directly (even in construction we wrap and unwrap them)
            // so it should be impossible to get here with a wrapper that was created via ISourceGenerator.AsIncrementalGenerator.
            // If we ever do have such an API, we will need to make sure that the source extension is updated as part of adding it to the driver.
            Debug.Assert(_sourceExtension != DummySourceExtension);

            GeneratorInitializationContext generatorInitContext = new GeneratorInitializationContext(CancellationToken.None);
#pragma warning disable CS0618 // Type or member is obsolete
            SourceGenerator.Initialize(generatorInitContext);
#pragma warning restore CS0618 // Type or member is obsolete

            if (generatorInitContext.Callbacks.PostInitCallback is object)
            {
                context.RegisterPostInitializationOutput(generatorInitContext.Callbacks.PostInitCallback);
            }

            var contextBuilderSource = context.CompilationProvider
                                        .Select((c, _) => new GeneratorContextBuilder(c))
                                        .Combine(context.ParseOptionsProvider).Select((p, _) => p.Item1 with { ParseOptions = p.Item2 })
                                        .Combine(context.AnalyzerConfigOptionsProvider).Select((p, _) => p.Item1 with { ConfigOptions = p.Item2 })
                                        .Combine(context.AdditionalTextsProvider.Collect()).Select((p, _) => p.Item1 with { AdditionalTexts = p.Item2 });

            var syntaxContextReceiverCreator = generatorInitContext.Callbacks.SyntaxContextReceiverCreator;
            if (syntaxContextReceiverCreator is object)
            {
                contextBuilderSource = contextBuilderSource
                                       .Combine(context.SyntaxProvider.CreateSyntaxReceiverProvider(syntaxContextReceiverCreator))
                                       .Select((p, _) => p.Item1 with { Receiver = p.Item2 });
            }

            context.RegisterSourceOutput(contextBuilderSource, (productionContext, contextBuilder) =>
            {
                var generatorExecutionContext = contextBuilder.ToExecutionContext(_sourceExtension, productionContext.ChecksumAlgorithm, productionContext.CancellationToken);
#pragma warning disable CS0618 // Type or member is obsolete
                SourceGenerator.Execute(generatorExecutionContext);
#pragma warning restore CS0618 // Type or member is obsolete

                // copy the contents of the old context to the new
                generatorExecutionContext.CopyToProductionContext(productionContext);
                generatorExecutionContext.Free();
            });
        }

        internal record GeneratorContextBuilder(Compilation Compilation)
        {
            public ParseOptions? ParseOptions;

            public ImmutableArray<AdditionalText> AdditionalTexts;

            public Diagnostics.AnalyzerConfigOptionsProvider? ConfigOptions;

            public ISyntaxContextReceiver? Receiver;

            public GeneratorExecutionContext ToExecutionContext(string sourceExtension, SourceHashAlgorithm checksumAlgorithm, CancellationToken cancellationToken)
            {
                Debug.Assert(ParseOptions is object && ConfigOptions is object);
                return new GeneratorExecutionContext(Compilation, ParseOptions, AdditionalTexts, ConfigOptions, Receiver, sourceExtension, checksumAlgorithm, cancellationToken);
            }
        }
    }
}
