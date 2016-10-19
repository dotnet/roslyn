// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Serialization;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// builder to create custom asset which is not part of solution but want to participate in ISolutionSynchronizationService
    /// </summary>
    internal class CustomAssetBuilder
    {
        private readonly Serializer _serializer;

        public CustomAssetBuilder(Solution solution)
        {
            _serializer = new Serializer(solution.Workspace.Services);
        }

        public CustomAsset Build(OptionSet options, string language, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return new SimpleCustomAsset(WellKnownSynchronizationKinds.OptionSet,
                (writer, cancellationTokenOnStreamWriting) =>
                    _serializer.SerializeOptionSet(options, language, writer, cancellationTokenOnStreamWriting));
        }

        public CustomAsset Build(AnalyzerReference reference, CancellationToken cancellationToken)
        {
            return new WorkspaceAnalyzerReferenceAsset(reference, _serializer);
        }
    }
}
