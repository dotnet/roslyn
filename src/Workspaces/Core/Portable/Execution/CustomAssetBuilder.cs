// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
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

        public CustomAsset Build(AnalyzerReference reference, CancellationToken cancellationToken)
            => WorkspaceAnalyzerReferenceAsset.Create(reference, _serializer, _hostSerializationService, cancellationToken);
    }
}
