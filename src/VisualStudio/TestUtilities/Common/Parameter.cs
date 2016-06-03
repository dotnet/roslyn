// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Roslyn.VisualStudio.Test.Utilities.Common
{
    [Serializable]
    public class Parameter : IEquatable<Parameter>
    {
        public string Name { get; set; }
        public string Documentation { get; set; }

        public Parameter()
        {
        }

        public Parameter(IParameter actual)
        {
            Name = actual.Name;
            if (string.IsNullOrEmpty(Name))
            {
                Name = null;
            }

            Documentation = actual.Documentation;
            if (string.IsNullOrEmpty(Documentation))
            {
                Documentation = null;
            }
        }

        public bool Equals(Parameter other)
        {
            if (other == null)
            {
                return false;
            }

            return Name == other.Name && Documentation == other.Documentation;
        }

        public override bool Equals(object obj)
        {
            Parameter other = obj as Parameter;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return (Name ?? string.Empty).GetHashCode()
                 ^ (Documentation ?? string.Empty).GetHashCode();
        }

        public override string ToString()
        {
            return Name + " (" + Documentation + ")";
        }
    }
}
