// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

#nullable enable
namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Context passed to a source generator when <see cref="ISourceGenerator.Execute(GeneratorExecutionContext)"/> is called
    /// </summary>
    public readonly struct GeneratorExecutionContext
    {
        private readonly DiagnosticBag _diagnostics;

        private readonly AdditionalSourcesCollection _additionalSources;

        internal GeneratorExecutionContext(Compilation compilation, ParseOptions parseOptions, ImmutableArray<AdditionalText> additionalTexts, AnalyzerConfigOptionsProvider optionsProvider, ISyntaxReceiver? syntaxReceiver, CancellationToken cancellationToken = default)
        {
            Compilation = compilation;
            ParseOptions = parseOptions;
            AdditionalFiles = additionalTexts;
            AnalyzerConfigOptions = optionsProvider;
            SyntaxReceiver = syntaxReceiver;
            CancellationToken = cancellationToken;
            _additionalSources = new AdditionalSourcesCollection();
            _diagnostics = new DiagnosticBag();
        }

        /// <summary>
        /// Get the current <see cref="Compilation"/> at the time of execution.
        /// </summary>
        /// <remarks>
        /// This compilation contains only the user supplied code; other generated code is not
        /// available. As user code can depend on the results of generation, it is possible that
        /// this compilation will contain errors.
        /// </remarks>
        public Compilation Compilation { get; }

        /// <summary>
        /// Get the <see cref="ParseOptions"/> that will be used to parse any added sources.
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
        /// A <see cref="CancellationToken"/> that can be checked to see if the generation should be cancelled.
        /// </summary>
        public CancellationToken CancellationToken { get; }

        /// <summary>
        /// Adds source code in the form of a <see cref="string"/> to the compilation.
        /// </summary>
        /// <param name="hintName">An identifier that can be used to reference this source text, must be unique within this generator</param>
        /// <param name="source">The source code to be add to the compilation</param>
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
        /// A <see cref="CancellationToken"/> that can be checked to see if the initialization should be cancelled.
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
            CheckIsEmpty(InfoBuilder.SyntaxReceiverCreator);
            InfoBuilder.SyntaxReceiverCreator = receiverCreator;
        }

        private static void CheckIsEmpty<T>(T x)
        {
            if (x is object)
            {
                throw new InvalidOperationException(string.Format(CodeAnalysisResources.Single_type_per_generator_0, typeof(T).Name));
            }
        }
    }

    internal readonly struct GeneratorEditContext
    {
        internal GeneratorEditContext(ImmutableArray<GeneratedSourceText> sources, CancellationToken cancellationToken = default)
        {
            AdditionalSources = new AdditionalSourcesCollection(sources);
            CancellationToken = cancellationToken;
        }

        public CancellationToken CancellationToken { get; }

        public AdditionalSourcesCollection AdditionalSources { get; }
    }
}
