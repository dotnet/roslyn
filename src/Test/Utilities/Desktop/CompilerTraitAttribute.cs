// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    [TraitDiscoverer("Microsoft.CodeAnalysis.Test.Utilities.CompilerTraitDiscoverer", assemblyName: "Roslyn.Test.Utilities.Desktop")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class CompilerTraitAttribute : Attribute, ITraitAttribute
    {
        public CompilerFeature[] Features { get; }

        public CompilerTraitAttribute(params CompilerFeature[] features)
        {
            Features = features;
        }
    }
}
