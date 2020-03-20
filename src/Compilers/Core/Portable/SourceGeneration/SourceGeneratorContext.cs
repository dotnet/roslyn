// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

#nullable enable
namespace Microsoft.CodeAnalysis
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0016:Add public types and members to the declared API", Justification = "In Progress")]
    public readonly struct SourceGeneratorContext
    {
        private readonly DiagnosticBag _diagnostics;

        internal SourceGeneratorContext(Compilation compilation, AnalyzerOptions options, ISyntaxReceiver? syntaxReceiver, DiagnosticBag diagnostics, CancellationToken cancellationToken = default)
        {
            Compilation = compilation;
            AnalyzerOptions = options;
            SyntaxReceiver = syntaxReceiver;
            CancellationToken = cancellationToken;
            AdditionalSources = new AdditionalSourcesCollection();
            _diagnostics = diagnostics;
        }

        public Compilation Compilation { get; }

        // PROTOTYPE: replace AnalyzerOptions with an differently named type that is otherwise identical.
        // The concern being that something added to one isn't necessarily applicable to the other.
        public AnalyzerOptions AnalyzerOptions { get; }

        public ISyntaxReceiver? SyntaxReceiver { get; }

        public CancellationToken CancellationToken { get; }

        public AdditionalSourcesCollection AdditionalSources { get; }

        public void ReportDiagnostic(Diagnostic diagnostic) => _diagnostics.Add(diagnostic);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0016:Add public types and members to the declared API", Justification = "In Progress")]
    public struct InitializationContext
    {
        internal InitializationContext(CancellationToken cancellationToken = default)
        {
            CancellationToken = cancellationToken;
            InfoBuilder = new GeneratorInfo.Builder();
        }

        public CancellationToken CancellationToken { get; }

        internal GeneratorInfo.Builder InfoBuilder { get; }

        public void RegisterForAdditionalFileChanges(EditCallback<AdditionalFileEdit> callback)
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
        /// During <see cref="ISourceGenerator.Execute(SourceGeneratorContext)"/> the generator can obtain the <see cref="ISyntaxReceiver"/> instance that was
        /// created by accessing the <see cref="SourceGeneratorContext.SyntaxReceiver"/> property. Any information that was collected by the receiver can be
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0016:Add public types and members to the declared API", Justification = "In progress")]
    // PROTOTYPE: this is going to need to track the input and output compilations that occured
    public readonly struct EditContext
    {
        internal EditContext(ImmutableArray<GeneratedSourceText> sources, CancellationToken cancellationToken = default)
        {
            AdditionalSources = new AdditionalSourcesCollection(sources);
            CancellationToken = cancellationToken;
        }

        public CancellationToken CancellationToken { get; }

        public AdditionalSourcesCollection AdditionalSources { get; }
    }
}
