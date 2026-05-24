// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

#pragma warning disable RSEXPERIMENTAL007 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

namespace Microsoft.CodeAnalysis
{
    internal sealed class PreCompilationSourceOutputNode<TInput> : AbstractSourceOutputNode<TInput>
    {
        private readonly Action<PreCompilationSourceProductionContext, TInput, CancellationToken> _action;

        public PreCompilationSourceOutputNode(IIncrementalGeneratorNode<TInput> source, Action<PreCompilationSourceProductionContext, TInput, CancellationToken> action, string sourceExtension)
            : base(source, sourceExtension)
        {
            _action = action;
        }

        public override IncrementalGeneratorOutputKind Kind => IncrementalGeneratorOutputKind.PreCompilation;

        protected override string StepName => WellKnownGeneratorOutputs.PreCompilationSourceOutput;

        protected override void InvokeUserAction(AdditionalSourcesCollection sources, DiagnosticBag diagnostics, DriverStateTable.Builder graphState, TInput item, CancellationToken cancellationToken)
        {
            var context = new PreCompilationSourceProductionContext(sources, graphState.DriverState.ChecksumAlgorithm, cancellationToken);
            _action(context, item, cancellationToken);
        }
    }
}
