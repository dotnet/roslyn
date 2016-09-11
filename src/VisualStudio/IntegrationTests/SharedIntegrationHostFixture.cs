// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Roslyn.VisualStudio.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests
{
    [CollectionDefinition(nameof(SharedIntegrationHostFixture))]
    public sealed class SharedIntegrationHostFixture : ICollectionFixture<VisualStudioInstanceFactory>
    {
        public const string RoslynLanguageServicesPackageId = "0b5e8ddb-f12d-4131-a71d-77acc26a798f";
        public const string VisualStudioIntegrationFrameworkPackageId = "d0122878-51f1-4b36-95ec-dec2079a2a84";

        public static readonly ImmutableHashSet<string> RequiredPackageIds = ImmutableHashSet.Create(VisualStudioIntegrationFrameworkPackageId, RoslynLanguageServicesPackageId);

        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
