// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Structure
{
    internal abstract class BlockStructureProvider
    {
        public abstract Task ProvideBlockStructureAsync(BlockStructureContext context);

        public virtual void ProvideBlockStructure(BlockStructureContext context)
            => ProvideBlockStructureAsync(context).Wait(context.CancellationToken);
    }
}
