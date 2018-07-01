// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests
{
    [CollectionDefinition(nameof(SharedIntegrationHostFixture), DisableParallelization = true)]
    public sealed class SharedIntegrationHostFixture : ICollectionFixture<VisualStudioInstanceFactory>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
