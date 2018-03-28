// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
