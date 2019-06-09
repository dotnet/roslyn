// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Serialization;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// builder to create custom asset which is not part of solution but want to participate in <see cref="IRemotableDataService"/>
    /// </summary>
    internal class CustomAssetBuilder
    {
        private readonly ISerializerService _serializer;
        private readonly IReferenceSerializationService _hostSerializationService;

        public CustomAssetBuilder(Solution solution) : this(solution.Workspace)
        {
        }

        public CustomAssetBuilder(Workspace workspace) : this(workspace.Services)
        {
        }

        public CustomAssetBuilder(HostWorkspaceServices services)
        {
            _serializer = services.GetService<ISerializerService>();
            _hostSerializationService = services.GetService<IReferenceSerializationService>();
        }

        public CustomAsset Build(OptionSet options, string language, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return new SimpleCustomAsset(WellKnownSynchronizationKind.OptionSet,
                (writer, cancellationTokenOnStreamWriting) =>
                    _serializer.SerializeOptionSet(options, language, writer, cancellationTokenOnStreamWriting));
        }

        public CustomAsset Build(AnalyzerReference reference, CancellationToken cancellationToken)
        {
            return WorkspaceAnalyzerReferenceAsset.Create(reference, _serializer, _hostSerializationService, cancellationToken);
        }
    }
}
