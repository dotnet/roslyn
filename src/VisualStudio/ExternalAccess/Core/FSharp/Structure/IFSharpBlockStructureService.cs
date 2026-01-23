// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Structure;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Structure;
#endif

internal interface IFSharpBlockStructureService
{
    Task<FSharpBlockStructure> GetBlockStructureAsync(Document document, CancellationToken cancellationToken);
}
