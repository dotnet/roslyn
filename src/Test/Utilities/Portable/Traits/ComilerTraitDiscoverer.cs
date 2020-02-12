// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Roslyn.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public sealed class CompilerTraitDiscoverer : ITraitDiscoverer
    {
        public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
        {
            var array = (CompilerFeature[])traitAttribute.GetConstructorArguments().Single();
            foreach (var feature in array)
            {
                var value = feature.ToString();
                yield return new KeyValuePair<string, string>("Compiler", value);
            }
        }
    }
}
