// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
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
        Checksum CreateChecksum(AnalyzerReference reference, bool usePathFromAssembly, CancellationToken cancellationToken);

        void WriteTo(Encoding encoding, ObjectWriter writer, CancellationToken cancellationToken);
        void WriteTo(MetadataReference reference, ObjectWriter writer, CancellationToken cancellationToken);
        void WriteTo(AnalyzerReference reference, ObjectWriter writer, bool usePathFromAssembly, CancellationToken cancellationToken);

        Encoding ReadEncodingFrom(ObjectReader reader, CancellationToken cancellationToken);
        MetadataReference ReadMetadataReferenceFrom(ObjectReader reader, CancellationToken cancellationToken);
        AnalyzerReference ReadAnalyzerReferenceFrom(ObjectReader reader, CancellationToken cancellationToken);
    }
}
