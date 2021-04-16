// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
            // PROTOTYPE(source-generators):
            // we'll call initialize on the underlying generator and pull out any info that is needed
            // then we'll set PostInit etc on our context and register a pipleine that
            // executes the old generator in the new framework

            GeneratorInitializationContext oldContext = new GeneratorInitializationContext(context.CancellationToken);
            SourceGenerator.Initialize(oldContext);

            context.InfoBuilder.PostInitCallback = oldContext.InfoBuilder.PostInitCallback;
            context.InfoBuilder.SyntaxContextReceiverCreator = oldContext.InfoBuilder.SyntaxContextReceiverCreator;

            context.RegisterExecutionPipeline((ctx) =>
            {
                var context = ctx.Sources.Compilation
                                         .Transform(c => new GeneratorContextBuilder(c))
                                         .Transform(c => c with { Options = c.Compilation.SyntaxTrees.First().Options }) // PROTOTYPE(source-generators): we should make an input source for the parse options
                                         .Join(ctx.Sources.AnalyzerConfigOptions).Transform(p => p.Item1 with { ConfigOptions = p.Item2.FirstOrDefault() })
                                         .Join(ctx.Sources.SyntaxReceiver).Transform(p => p.Item1 with { Receiver = p.Item2.FirstOrDefault() })
                                         .Join(ctx.Sources.AdditionalTexts).Transform(p => p.Item1 with { AdditionalTexts = p.Item2.ToImmutableArray() });


                var output = context.GenerateSource((context, contextBuilder) =>
                {

                    // PROTOTYPE(source-generators): VB extensions
                    AdditionalSourcesCollection asc = new AdditionalSourcesCollection(".cs");

                    // PROTOTYPE(source-generators): options/additionaltexts/configoptions
                    var oldContext = contextBuilder.ToExecutionContext(asc, context.CancellationToken);

                    // PROTOTYPE(source-generators):If this throws, we'll wrap it in a user func as expected. We probably *should* do that for the rest of this code though
                    // So we probably need an internal version that doesn't wrap it? Maybe we can just construct the nodes manually.
                    SourceGenerator.Execute(oldContext);

                    // PROTOTYPE(source-generators): we should make the internals visible so we can just add directly here
                    (var source, var diagnostics) = oldContext.ToImmutableAndFree();
                    foreach (var s in source)
                    {
                        context.AddSource(s.HintName, s.Text);
                    }
                    foreach (var d in diagnostics)
                    {
                        context.ReportDiagnostic(d);
                    }
                });
                ctx.RegisterOutput(output);


                //ctx.Sources.Compilation

                //// https://github.com/dotnet/roslyn/issues/42629: should be possible to parallelize this
                //for (int i = 0; i < state.Generators.Length; i++)
                //{
                //    var generator = state.Generators[i];
                //    var generatorState = stateBuilder[i];

                //    // don't try and generate if initialization or syntax walk failed
                //    if (generatorState.Exception is object)
                //    {
                //        continue;
                //    }
                //    Debug.Assert(generatorState.Info.Initialized);

                //    //// we create a new context for each run of the generator. We'll never re-use existing state, only replace anything we have 
                //    //var context = new GeneratorExecutionContext(compilation, state.ParseOptions, state.AdditionalTexts.NullToEmpty(), state.OptionsProvider, generatorState.SyntaxReceiver, CreateSourcesCollection(), cancellationToken);
                //    //try
                //    //{
                //    //    generator.Execute(context);
                //    //}
                //    //catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
                //    //{
                //    //    stateBuilder[i] = SetGeneratorException(MessageProvider, generatorState, generator, e, diagnosticsBag);
                //    //    context.Free();
                //    //    continue;
                //    //}

                //    //(var sources, var diagnostics) = context.ToImmutableAndFree();
                //    //stateBuilder[i] = new GeneratorState(generatorState.Info, generatorState.PostInitTrees, generatorState.OutputNodes, ParseAdditionalSources(generator, sources, cancellationToken), diagnostics);
                //    //diagnosticsBag?.AddRange(diagnostics);
                //}
                //state = state.With(generatorStates: stateBuilder.ToImmutableAndFree());
            });
        }

        record GeneratorContextBuilder(Compilation Compilation)
        {
            public ParseOptions? Options;

            public ImmutableArray<AdditionalText> AdditionalTexts;

            public Diagnostics.AnalyzerConfigOptionsProvider? ConfigOptions;

            public ISyntaxContextReceiver? Receiver;

            public GeneratorExecutionContext ToExecutionContext(AdditionalSourcesCollection asc, CancellationToken cancellationToken)
            {
                Debug.Assert(Options is object && ConfigOptions is object);
                return new GeneratorExecutionContext(Compilation, Options, AdditionalTexts, ConfigOptions, Receiver, asc, cancellationToken);

            }

        }
    }
}
