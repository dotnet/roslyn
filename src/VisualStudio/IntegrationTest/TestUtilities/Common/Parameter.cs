// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Language.Intellisense;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.IntegrationTest.Utilities.Common
{
    [Serializable]
    public class Parameter : IEquatable<Parameter>
    {
        public string Name { get; set; }

        public string Documentation { get; set; }

        public Parameter() { }

        public Parameter(IParameter actual)
        {
            Name = actual.Name;
            Documentation = actual.Documentation;
        }

        public bool Equals(Parameter other)
        {
            if (other == null)
            {
                return false;
            }

            return Comparison.AreStringValuesEqual(Name, other.Name)
                && Comparison.AreStringValuesEqual(Documentation, other.Documentation);
        }

        public override bool Equals(object obj)
            => Equals(obj as Parameter);

        public override int GetHashCode()
            => Hash.Combine(Name, Hash.Combine(Documentation, 0));

        public override string ToString()
            => !string.IsNullOrEmpty(Documentation) ? $"{Name} ({Documentation})" : Name;
    }
}
