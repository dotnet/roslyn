// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Execution
{
    /// <summary>
    /// This deals with how to serialize/deserialize references that we have multiple implementations 
    /// between different layers such as workspace, host - ex, VS.
    /// </summary>
    internal interface IReferenceSerializationService : IWorkspaceService
    {
        Checksum CreateChecksum(MetadataReference reference, CancellationToken cancellationToken);
        Checksum CreateChecksum(AnalyzerReference reference, CancellationToken cancellationToken);

        void WriteTo(MetadataReference reference, ObjectWriter writer, CancellationToken cancellationToken);
        void WriteTo(AnalyzerReference reference, ObjectWriter writer, CancellationToken cancellationToken);

        MetadataReference ReadMetadataReferenceFrom(ObjectReader reader, CancellationToken cancellationToken);
        AnalyzerReference ReadAnalyzerReferenceFrom(ObjectReader reader, CancellationToken cancellationToken);
    }
}
