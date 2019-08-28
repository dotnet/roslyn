// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
