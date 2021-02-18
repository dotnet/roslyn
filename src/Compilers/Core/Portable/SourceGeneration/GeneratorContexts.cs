﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Context passed to a source generator when <see cref="ISourceGenerator.Execute(GeneratorExecutionContext)"/> is called
    /// </summary>
    public readonly struct GeneratorExecutionContext
    {
        private readonly DiagnosticBag _diagnostics;

        private readonly AdditionalSourcesCollection _additionalSources;

        internal GeneratorExecutionContext(Compilation compilation, ParseOptions parseOptions, ImmutableArray<AdditionalText> additionalTexts, AnalyzerConfigOptionsProvider optionsProvider, ISyntaxContextReceiver? syntaxReceiver, AdditionalSourcesCollection additionalSources, CancellationToken cancellationToken = default)
        {
            Compilation = compilation;
            ParseOptions = parseOptions;
            AdditionalFiles = additionalTexts;
            AnalyzerConfigOptions = optionsProvider;
            SyntaxReceiver = (syntaxReceiver as SyntaxContextReceiverAdaptor)?.Receiver;
            SyntaxContextReceiver = (syntaxReceiver is SyntaxContextReceiverAdaptor) ? null : syntaxReceiver;
            CancellationToken = cancellationToken;
            _additionalSources = additionalSources;
            _diagnostics = new DiagnosticBag();
        }

        /// <summary>
        /// Get the current <see cref="CodeAnalysis.Compilation"/> at the time of execution.
        /// </summary>
        /// <remarks>
        /// This compilation contains only the user supplied code; other generated code is not
        /// available. As user code can depend on the results of generation, it is possible that
        /// this compilation will contain errors.
        /// </remarks>
        public Compilation Compilation { get; }

        /// <summary>
        /// Get the <see cref="CodeAnalysis.ParseOptions"/> that will be used to parse any added sources.
        /// </summary>
        public ParseOptions ParseOptions { get; }

        /// <summary>
        /// A set of additional non-code text files that can be used by generators.
        /// </summary>
        public ImmutableArray<AdditionalText> AdditionalFiles { get; }

        /// <summary>
        /// Allows access to options provided by an analyzer config
        /// </summary>
        public AnalyzerConfigOptionsProvider AnalyzerConfigOptions { get; }

        /// <summary>
        /// If the generator registered an <see cref="ISyntaxReceiver"/> during initialization, this will be the instance created for this generation pass.
        /// </summary>
        public ISyntaxReceiver? SyntaxReceiver { get; }

        /// <summary>
        /// If the generator registered an <see cref="ISyntaxContextReceiver"/> during initialization, this will be the instance created for this generation pass.
        /// </summary>
        public ISyntaxContextReceiver? SyntaxContextReceiver { get; }

        /// <summary>
        /// A <see cref="System.Threading.CancellationToken"/> that can be checked to see if the generation should be cancelled.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        /// <summary>
        /// Adds source code in the form of a <see cref="string"/> to the compilation.
        /// </summary>
        /// <param name="hintName">An identifier that can be used to reference this source text, must be unique within this generator</param>
        /// <param name="source">The source code to add to the compilation</param>
        public void AddSource(string hintName, string source) => AddSource(hintName, SourceText.From(source, Encoding.UTF8));

        /// <summary>
        /// Adds a <see cref="SourceText"/> to the compilation
        /// </summary>
        /// <param name="hintName">An identifier that can be used to reference this source text, must be unique within this generator</param>
        /// <param name="sourceText">The <see cref="SourceText"/> to add to the compilation</param>
        public void AddSource(string hintName, SourceText sourceText) => _additionalSources.Add(hintName, sourceText);

        /// <summary>
        /// Adds a <see cref="Diagnostic"/> to the users compilation 
        /// </summary>
        /// <param name="diagnostic">The diagnostic that should be added to the compilation</param>
        /// <remarks>
        /// The severity of the diagnostic may cause the compilation to fail, depending on the <see cref="Compilation"/> settings.
        /// </remarks>
        public void ReportDiagnostic(Diagnostic diagnostic) => _diagnostics.Add(diagnostic);

        internal (ImmutableArray<GeneratedSourceText> sources, ImmutableArray<Diagnostic> diagnostics) ToImmutableAndFree()
            => (_additionalSources.ToImmutableAndFree(), _diagnostics.ToReadOnlyAndFree());
    }

    /// <summary>
    /// Context passed to a source generator when <see cref="ISourceGenerator.Initialize(GeneratorInitializationContext)"/> is called
    /// </summary>
    public struct GeneratorInitializationContext
    {
        internal GeneratorInitializationContext(CancellationToken cancellationToken = default)
        {
            CancellationToken = cancellationToken;
            InfoBuilder = new GeneratorInfo.Builder();
        }

        /// <summary>
        /// A <see cref="System.Threading.CancellationToken"/> that can be checked to see if the initialization should be cancelled.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        internal GeneratorInfo.Builder InfoBuilder { get; }

        internal void RegisterForAdditionalFileChanges(EditCallback<AdditionalFileEdit> callback)
        {
            CheckIsEmpty(InfoBuilder.EditCallback);
            InfoBuilder.EditCallback = callback;
        }

        /// <summary>
        /// Register a <see cref="SyntaxReceiverCreator"/> for this generator, which can be used to create an instance of an <see cref="ISyntaxReceiver"/>.
        /// </summary>
        /// <remarks>
        /// This method allows generators to be 'syntax aware'. Before each generation the <paramref name="receiverCreator"/> will be invoked to create
        /// an instance of <see cref="ISyntaxReceiver"/>. This receiver will have its <see cref="ISyntaxReceiver.OnVisitSyntaxNode(SyntaxNode)"/> 
        /// invoked for each syntax node in the compilation, allowing the receiver to build up information about the compilation before generation occurs.
        /// 
        /// During <see cref="ISourceGenerator.Execute(GeneratorExecutionContext)"/> the generator can obtain the <see cref="ISyntaxReceiver"/> instance that was
        /// created by accessing the <see cref="GeneratorExecutionContext.SyntaxReceiver"/> property. Any information that was collected by the receiver can be
        /// used to generate the final output.
        /// 
        /// A new instance of <see cref="ISyntaxReceiver"/> is created per-generation, meaning there is no need to manage the lifetime of the 
        /// receiver or its contents.
        /// </remarks>
        /// <param name="receiverCreator">A <see cref="SyntaxReceiverCreator"/> that can be invoked to create an instance of <see cref="ISyntaxReceiver"/></param>
        public void RegisterForSyntaxNotifications(SyntaxReceiverCreator receiverCreator)
        {
            CheckIsEmpty(InfoBuilder.SyntaxContextReceiverCreator, $"{nameof(SyntaxReceiverCreator)} / {nameof(SyntaxContextReceiverCreator)}");
            InfoBuilder.SyntaxContextReceiverCreator = SyntaxContextReceiverAdaptor.Create(receiverCreator);
        }

        /// <summary>
        /// Register a <see cref="SyntaxContextReceiverCreator"/> for this generator, which can be used to create an instance of an <see cref="ISyntaxContextReceiver"/>.
        /// </summary>
        /// <remarks>
        /// This method allows generators to be 'syntax aware'. Before each generation the <paramref name="receiverCreator"/> will be invoked to create
        /// an instance of <see cref="ISyntaxContextReceiver"/>. This receiver will have its <see cref="ISyntaxContextReceiver.OnVisitSyntaxNode(GeneratorSyntaxContext)"/> 
        /// invoked for each syntax node in the compilation, allowing the receiver to build up information about the compilation before generation occurs.
        /// 
        /// During <see cref="ISourceGenerator.Execute(GeneratorExecutionContext)"/> the generator can obtain the <see cref="ISyntaxContextReceiver"/> instance that was
        /// created by accessing the <see cref="GeneratorExecutionContext.SyntaxContextReceiver"/> property. Any information that was collected by the receiver can be
        /// used to generate the final output.
        /// 
        /// A new instance of <see cref="ISyntaxContextReceiver"/> is created prior to every call to <see cref="ISourceGenerator.Execute(GeneratorExecutionContext)"/>, 
        /// meaning there is no need to manage the lifetime of the receiver or its contents.
        /// </remarks>
        /// <param name="receiverCreator">A <see cref="SyntaxContextReceiverCreator"/> that can be invoked to create an instance of <see cref="ISyntaxContextReceiver"/></param>
        public void RegisterForSyntaxNotifications(SyntaxContextReceiverCreator receiverCreator)
        {
            CheckIsEmpty(InfoBuilder.SyntaxContextReceiverCreator, $"{nameof(SyntaxReceiverCreator)} / {nameof(SyntaxContextReceiverCreator)}");
            InfoBuilder.SyntaxContextReceiverCreator = receiverCreator;
        }

        /// <summary>
        /// Register a callback that is invoked after initialization.
        /// </summary>
        /// <remarks>
        /// This method allows a generator to opt-in to an extra phase in the generator lifecycle called PostInitialization. After being initialized
        /// any generators that have opted in will have their provided callback invoked with a <see cref="GeneratorPostInitializationContext"/> instance
        /// that can be used to alter the compilation that is provided to subsequent generator phases.
        /// 
        /// For example a generator may choose to add sources during PostInitialization. These will be added to the compilation before execution and
        /// will be visited by a registered <see cref="ISyntaxReceiver"/> and available for semantic analysis as part of the <see cref="GeneratorExecutionContext.Compilation"/>
        /// 
        /// Note that any sources added during PostInitialization <i>will</i> be visible to the later phases of other generators operation on the compilation. 
        /// </remarks>
        /// <param name="callback">An <see cref="Action{T}"/> that accepts a <see cref="GeneratorPostInitializationContext"/> that will be invoked after initialization.</param>
        public void RegisterForPostInitialization(Action<GeneratorPostInitializationContext> callback)
        {
            CheckIsEmpty(InfoBuilder.PostInitCallback);
            InfoBuilder.PostInitCallback = callback;
        }

        private static void CheckIsEmpty<T>(T x, string? typeName = null) where T : class?
        {
            if (x is object)
            {
                throw new InvalidOperationException(string.Format(CodeAnalysisResources.Single_type_per_generator_0, typeName ?? typeof(T).Name));
            }
        }
    }

    /// <summary>
    /// Context passed to an <see cref="ISyntaxContextReceiver"/> when <see cref="ISyntaxContextReceiver.OnVisitSyntaxNode(GeneratorSyntaxContext)"/> is called
    /// </summary>
    public readonly struct GeneratorSyntaxContext
    {
        internal GeneratorSyntaxContext(SyntaxNode node, SemanticModel semanticModel)
        {
            Node = node;
            SemanticModel = semanticModel;
        }

        /// <summary>
        /// The <see cref="SyntaxNode"/> currently being visited
        /// </summary>
        public SyntaxNode Node { get; }

        /// <summary>
        /// The <see cref="CodeAnalysis.SemanticModel" /> that can be queried to obtain information about <see cref="Node"/>.
        /// </summary>
        public SemanticModel SemanticModel { get; }
    }

    /// <summary>
    /// Context passed to a source generator when it has opted-in to PostInitialization via <see cref="GeneratorInitializationContext.RegisterForPostInitialization(Action{GeneratorPostInitializationContext})"/>
    /// </summary>
    public readonly struct GeneratorPostInitializationContext
    {
        private readonly AdditionalSourcesCollection _additionalSources;

        internal GeneratorPostInitializationContext(AdditionalSourcesCollection additionalSources, CancellationToken cancellationToken)
        {
            _additionalSources = additionalSources;
            CancellationToken = cancellationToken;
        }

        /// <summary>
        /// A <see cref="System.Threading.CancellationToken"/> that can be checked to see if the PostInitialization should be cancelled.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        /// <summary>
        /// Adds source code in the form of a <see cref="string"/> to the compilation that will be available during subsequent phases
        /// </summary>
        /// <param name="hintName">An identifier that can be used to reference this source text, must be unique within this generator</param>
        /// <param name="source">The source code to add to the compilation</param>
        public void AddSource(string hintName, string source) => AddSource(hintName, SourceText.From(source, Encoding.UTF8));

        /// <summary>
        /// Adds a <see cref="SourceText"/> to the compilation that will be available during subsequent phases
        /// </summary>
        /// <param name="hintName">An identifier that can be used to reference this source text, must be unique within this generator</param>
        /// <param name="sourceText">The <see cref="SourceText"/> to add to the compilation</param>
        public void AddSource(string hintName, SourceText sourceText) => _additionalSources.Add(hintName, sourceText);
    }

    internal readonly struct GeneratorEditContext
    {
        internal GeneratorEditContext(AdditionalSourcesCollection sources, CancellationToken cancellationToken = default)
        {
            AdditionalSources = sources;
            CancellationToken = cancellationToken;
        }

        public CancellationToken CancellationToken { get; }

        public AdditionalSourcesCollection AdditionalSources { get; }
    }
}
