// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Adapts an ISourceGenerator to an incremental generator that
    /// by providing an execution environment that matches the old one
    /// </summary>
    internal sealed class SourceGeneratorAdaptor : IIncrementalGenerator
    {
        internal ISourceGenerator SourceGenerator { get; }

        public SourceGeneratorAdaptor(ISourceGenerator generator)
        {
            SourceGenerator = generator;
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            GeneratorInitializationContext oldContext = new GeneratorInitializationContext(context.CancellationToken);
            SourceGenerator.Initialize(oldContext);

            context.InfoBuilder.PostInitCallback = oldContext.InfoBuilder.PostInitCallback;
            context.InfoBuilder.SyntaxContextReceiverCreator = oldContext.InfoBuilder.SyntaxContextReceiverCreator;

            context.RegisterExecutionPipeline((ctx) =>
            {
                var context = ctx.Sources.Compilation
                                         .Transform(c => new GeneratorContextBuilder(c))
                                         .Join(ctx.Sources.ParseOptions).Transform(p => p.Item1 with { ParseOptions = p.Item2.FirstOrDefault() })
                                         .Join(ctx.Sources.AnalyzerConfigOptions).Transform(p => p.Item1 with { ConfigOptions = p.Item2.FirstOrDefault() })
                                         .Join(ctx.Sources.CreateSyntaxReceiver()).Transform(p => p.Item1 with { Receiver = p.Item2.FirstOrDefault() })
                                         .Join(ctx.Sources.AdditionalTexts).Transform(p => p.Item1 with { AdditionalTexts = p.Item2.ToImmutableArray() });

                var output = context.GenerateSource((context, contextBuilder) =>
                {
                    var oldContext = contextBuilder.ToExecutionContext(context.CancellationToken);

                    // PROTOTYPE(source-generators):If this throws, we'll wrap it in a user func as expected. We probably *shouldn't* do that for the rest of this code though
                    // So we probably need an internal version that doesn't wrap it? Maybe we can just construct the nodes manually.
                    SourceGenerator.Execute(oldContext);

                    // copy the contents of the old context to the new
                    oldContext.CopyToProductionContext(context);
                    oldContext.Free();
                });
                ctx.RegisterOutput(output);
            });
        }

        internal record GeneratorContextBuilder(Compilation Compilation)
        {
            public ParseOptions? ParseOptions;

            public ImmutableArray<AdditionalText> AdditionalTexts;

            public Diagnostics.AnalyzerConfigOptionsProvider? ConfigOptions;

            public ISyntaxContextReceiver? Receiver;

            public GeneratorExecutionContext ToExecutionContext(CancellationToken cancellationToken)
            {
                Debug.Assert(ParseOptions is object && ConfigOptions is object);
                return new GeneratorExecutionContext(Compilation, ParseOptions, AdditionalTexts, ConfigOptions, Receiver, cancellationToken);
            }
        }
    }
}
