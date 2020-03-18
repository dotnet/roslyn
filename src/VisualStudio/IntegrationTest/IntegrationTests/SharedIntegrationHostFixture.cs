﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.VisualStudio.IntegrationTest.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.IntegrationTests
{
    [CollectionDefinition(nameof(SharedIntegrationHostFixture))]
    public sealed class SharedIntegrationHostFixture : ICollectionFixture<VisualStudioInstanceFactory>
    {
        public const string MSBuildPackageId = "Microsoft.Component.MSBuild";
        public const string Net46TargetingPackPackageId = "Microsoft.Net.Component.4.6.TargetingPack";
        public const string PortableLibraryPackageId = "Microsoft.VisualStudio.Component.PortableLibrary";
        public const string RoslynCompilerPackageId = "Microsoft.VisualStudio.Component.Roslyn.Compiler";
        public const string RoslynLanguageServicesPackageId = "Microsoft.VisualStudio.Component.Roslyn.LanguageServices";
        public const string VsSdkPackageId = "Microsoft.VisualStudio.Component.VSSDK";

        public static readonly ImmutableHashSet<string> RequiredPackageIds = ImmutableHashSet.Create(MSBuildPackageId, Net46TargetingPackPackageId, PortableLibraryPackageId, RoslynCompilerPackageId, RoslynLanguageServicesPackageId, VsSdkPackageId);

        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}
