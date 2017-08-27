// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Structure
{
    internal abstract class BlockStructureProvider
    {
        public abstract Task ProvideBlockStructureAsync(BlockStructureContext context);

        public virtual void ProvideBlockStructure(BlockStructureContext context)
        {
            ProvideBlockStructureAsync(context).Wait(context.CancellationToken);
        }
    }
}
