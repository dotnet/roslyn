// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Editor.CallstackExplorer
{
    internal class StackTraceResult : ParsedLine
    {
        private string _v;

        public StackTraceResult(string v)
        {
            _v = v;
        }

        internal override Task<ISymbol?> ResolveSymbolAsync(Solution solution, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}
