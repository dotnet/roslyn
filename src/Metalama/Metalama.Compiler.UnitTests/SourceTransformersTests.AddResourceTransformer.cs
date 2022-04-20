// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Metalama.Compiler.UnitTests;

public partial class SourceTransformersTests
{
    private class AddResourceTransformer : ISourceTransformer
    {
        private readonly ManagedResource[] _resources;

        public AddResourceTransformer(ManagedResource[] resources)
        {
            _resources = resources;
        }

        public void Execute(TransformerContext context)
        {
            context.AddResources(this._resources);
        }
    }
}
