// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

internal partial class SolutionState
{
    private partial class SkeletonReferenceCache
    {
        /// <summary>
        /// Version number we mix into the checksums we create to ensure that if our serialization format changes we
        /// automatically will fail 
        /// </summary>
        private static readonly Checksum s_persistenceVersion = Checksum.Create("1");

        private Task WriteToPersistentStorageAsync(
            Workspace workspace,
            ProjectId projectId,
            Checksum checksum,
            Stream peStream,
            Stream xmlDocumentationStream,
            string? assemblyName,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
