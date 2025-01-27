// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    [TraitDiscoverer("Microsoft.CodeAnalysis.Test.Utilities.CompilerTraitDiscoverer", assemblyName: "Microsoft.CodeAnalysis.Test.Utilities")]
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
