// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator.UnitTests
{
    internal sealed class TypeComparer : IEqualityComparer<TypeSignature>
    {
        internal static readonly TypeComparer Instance = new TypeComparer();

        public bool Equals(TypeSignature x, TypeSignature y)
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
                case TypeSignatureKind.QualifiedType:
                    {
                        var xQualified = (QualifiedTypeSignature)x;
                        var yQualified = (QualifiedTypeSignature)y;
                        return Equals(xQualified.Qualifier, yQualified.Qualifier) &&
                            Equals(xQualified.Name, yQualified.Name);
                    }
                case TypeSignatureKind.GenericType:
                    {
                        var xGeneric = (GenericTypeSignature)x;
                        var yGeneric = (GenericTypeSignature)y;
                        return Equals(xGeneric.QualifiedName, yGeneric.QualifiedName) &&
                            xGeneric.TypeArguments.SequenceEqual(yGeneric.TypeArguments, this);
                    }
                case TypeSignatureKind.ArrayType:
                    {
                        var xArray = (ArrayTypeSignature)x;
                        var yArray = (ArrayTypeSignature)y;
                        return Equals(xArray.ElementType, yArray.ElementType) &&
                            xArray.Rank == yArray.Rank;
                    }
                case TypeSignatureKind.PointerType:
                    {
                        var xPointer = (PointerTypeSignature)x;
                        var yPointer = (PointerTypeSignature)y;
                        return Equals(xPointer.PointedAtType, yPointer.PointedAtType);
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        public int GetHashCode(TypeSignature obj)
        {
            throw new NotImplementedException();
        }
    }
}
