// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using Xunit;
using Xunit.v3;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class CompilerTraitAttribute : Attribute, ITraitAttribute
    {
        public CompilerFeature[] Features { get; }

        public CompilerTraitAttribute(params CompilerFeature[] features)
        {
            Features = features;
        }

        public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits()
        {
            var traits = new List<KeyValuePair<string, string>>(Features.Length);
            foreach (var feature in Features)
            {
                traits.Add(new KeyValuePair<string, string>("Compiler", feature.ToString()));
            }

            return traits;
        }
    }
}
