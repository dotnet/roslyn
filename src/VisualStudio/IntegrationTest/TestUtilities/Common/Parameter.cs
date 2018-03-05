// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
