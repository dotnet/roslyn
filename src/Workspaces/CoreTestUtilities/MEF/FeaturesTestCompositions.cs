// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public static class FeaturesTestCompositions
    {
        public static readonly TestComposition Empty = new TestComposition(ImmutableHashSet<Assembly>.Empty, ImmutableHashSet<Type>.Empty, vsMef: false);

        public static readonly TestComposition Features = Empty.WithAdditionalParts(
            MefHostServices.DefaultAssemblies,
            Array.Empty<Type>());

        public static readonly TestComposition ServerFeatures = Empty.WithAdditionalParts(
            RoslynServices.RemoteHostAssemblies,
            Array.Empty<Type>());
    }
}
