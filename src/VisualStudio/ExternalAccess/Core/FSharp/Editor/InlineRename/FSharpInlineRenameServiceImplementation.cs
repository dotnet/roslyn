// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Editor;
#else
namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor;
#endif

internal abstract class FSharpInlineRenameServiceImplementation
{
    public abstract Task<FSharpInlineRenameInfo?> GetRenameInfoAsync(Document document, int position, CancellationToken cancellationToken);
}
