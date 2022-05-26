// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
{
    internal sealed class NameComparer : IEqualityComparer<Name>
    {
        internal static readonly NameComparer Instance = new NameComparer();

        public bool Equals(Name x, Name y)
        {
            if (x == null)
            {
                return y == null;
            }
            if (y == null)
            {
                return false;
            }
            if (x.Kind != y.Kind)
            {
                return false;
            }
            switch (x.Kind)
            {
                case NameKind.QualifiedName:
                    {
                        var xQualified = (QualifiedName)x;
                        var yQualified = (QualifiedName)y;
                        return Equals(xQualified.Qualifier, yQualified.Qualifier) &&
                            Equals(xQualified.Name, yQualified.Name);
                    }
                case NameKind.GenericName:
                    {
                        var xGeneric = (GenericName)x;
                        var yGeneric = (GenericName)y;
                        return Equals(xGeneric.QualifiedName, yGeneric.QualifiedName) &&
                            xGeneric.TypeParameters.SequenceEqual(yGeneric.TypeParameters);
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        public int GetHashCode(Name obj)
        {
            throw new NotImplementedException();
        }
    }
}
