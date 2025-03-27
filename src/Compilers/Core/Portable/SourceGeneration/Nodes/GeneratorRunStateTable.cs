// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed class GeneratorRunStateTable
    {
        private GeneratorRunStateTable(ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> executedSteps, ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> outputSteps)
        {
            ExecutedSteps = executedSteps;
            OutputSteps = outputSteps;
        }

        public ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> ExecutedSteps { get; }

        public ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> OutputSteps { get; }

        public sealed class Builder
        {
            private readonly Dictionary<string, HashSet<IncrementalGeneratorRunStep>>? _namedSteps;
            private readonly Dictionary<string, HashSet<IncrementalGeneratorRunStep>>? _outputSteps;

            public Builder(bool recordingExecutedSteps)
            {
                if (recordingExecutedSteps)
                {
                    _namedSteps = new Dictionary<string, HashSet<IncrementalGeneratorRunStep>>();
                    _outputSteps = new Dictionary<string, HashSet<IncrementalGeneratorRunStep>>();
                }
            }

            public void RecordStepsFromOutputNodeUpdate(IStateTable table)
            {
                Debug.Assert(RecordingExecutedSteps);
                Debug.Assert(table.HasTrackedSteps);
                foreach (var step in table.Steps)
                {
                    RecordStepTree(step, addToOutputSteps: true);
                }
            }

            [MemberNotNullWhen(true, nameof(_namedSteps), nameof(_outputSteps))]
            public bool RecordingExecutedSteps => _namedSteps is not null;

            public GeneratorRunStateTable ToImmutableAndFree()
            {
                return new GeneratorRunStateTable(StepCollectionToImmutable(_namedSteps), StepCollectionToImmutable(_outputSteps));
            }

            private static ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> StepCollectionToImmutable(Dictionary<string, HashSet<IncrementalGeneratorRunStep>>? builder)
            {
                if (builder is null)
                {
                    return ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>>.Empty;
                }

                ImmutableDictionary<string, ImmutableArray<IncrementalGeneratorRunStep>>.Builder resultBuilder = ImmutableDictionary.CreateBuilder<string, ImmutableArray<IncrementalGeneratorRunStep>>();

                foreach (var stepsByName in builder)
                {
                    resultBuilder.Add(stepsByName.Key, stepsByName.Value.ToImmutableArrayOrEmpty());
                }

                return resultBuilder.ToImmutable();
            }

            private void RecordStepTree(IncrementalGeneratorRunStep step, bool addToOutputSteps)
            {
                Debug.Assert(RecordingExecutedSteps);
                foreach (var (inputStep, _) in step.Inputs)
                {
                    RecordStepTree(inputStep, addToOutputSteps: false);
                }

                // If the step name is not null, we record it in the easily accessible collections.
                // Otherwise, it will be available only through graph traversal.
                if (step.Name is not null)
                {
                    addToNamedStepCollection(_namedSteps, step);
                    if (addToOutputSteps)
                    {
                        addToNamedStepCollection(_outputSteps, step);
                    }
                }

                static void addToNamedStepCollection(Dictionary<string, HashSet<IncrementalGeneratorRunStep>> stepCollectionBuilder, IncrementalGeneratorRunStep step)
                {
                    Debug.Assert(step.Name is not null);
                    if (!stepCollectionBuilder.TryGetValue(step.Name, out var stepsByName))
                    {
                        stepsByName = new HashSet<IncrementalGeneratorRunStep>();
                        stepCollectionBuilder.Add(step.Name, stepsByName);
                    }
                    stepsByName.Add(step);
                }
            }
        }
    }
}
