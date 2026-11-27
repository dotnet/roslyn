// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;

namespace Microsoft.CodeAnalysis
{
    internal sealed class SourceOutputNode<TInput> : AbstractSourceOutputNode<TInput>
    {
        private readonly Action<SourceProductionContext, TInput, CancellationToken> _action;
        private readonly IncrementalGeneratorOutputKind _outputKind;

        public SourceOutputNode(IIncrementalGeneratorNode<TInput> source, Action<SourceProductionContext, TInput, CancellationToken> action, IncrementalGeneratorOutputKind outputKind, string sourceExtension)
            : base(source, sourceExtension)
        {
            Debug.Assert(outputKind is IncrementalGeneratorOutputKind.Source or IncrementalGeneratorOutputKind.Implementation);
            _action = action;
            _outputKind = outputKind;
        }

        public override IncrementalGeneratorOutputKind Kind => _outputKind;

        protected override string StepName => Kind == IncrementalGeneratorOutputKind.Source
            ? WellKnownGeneratorOutputs.SourceOutput
            : WellKnownGeneratorOutputs.ImplementationSourceOutput;

        protected override void InvokeUserAction(AdditionalSourcesCollection sources, DiagnosticBag diagnostics, DriverStateTable.Builder graphState, TInput item, CancellationToken cancellationToken)
        {
            var context = new SourceProductionContext(sources, diagnostics, graphState.Compilation, graphState.DriverState.ChecksumAlgorithm, cancellationToken);
            _action(context, item, cancellationToken);
        }
    }
}
