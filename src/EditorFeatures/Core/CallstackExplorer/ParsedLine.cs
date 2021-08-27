using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.CallstackExplorer;
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Editor.CallstackExplorer
{
    internal abstract class ParsedLine
    {
        internal abstract Task<ISymbol?> ResolveSymbolAsync(Solution solution, CancellationToken cancellationToken);
    }
}
