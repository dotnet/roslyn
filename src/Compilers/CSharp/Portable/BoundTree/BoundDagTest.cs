// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    partial class BoundDagTest
    {
        public override bool Equals([NotNullWhen(true)] object? obj) => this.Equals(obj as BoundDagTest);

        private bool Equals(BoundDagTest? other)
        {
            if (other is null || this.Kind != other.Kind)
                return false;
            if (this == other)
                return true;

            switch (this, other)
            {
                case (BoundDagTypeTest x, BoundDagTypeTest y):
                    return x.Type.Equals(y.Type, TypeCompareKind.AllIgnoreOptions);
                case (BoundDagNonNullTest x, BoundDagNonNullTest y):
                    return x.IsExplicitTest == y.IsExplicitTest;
                case (BoundDagExplicitNullTest x, BoundDagExplicitNullTest y):
                    return true;
                case (BoundDagValueTest x, BoundDagValueTest y):
                    return x.Value.Equals(y.Value);
                case (BoundDagRelationalTest x, BoundDagRelationalTest y):
                    return x.Relation == y.Relation && x.Value.Equals(y.Value);
                default:
                    throw ExceptionUtilities.UnexpectedValue(this);
            }
        }

        public override int GetHashCode()
        {
            return Hash.Combine(Kind.GetHashCode(), Input.GetHashCode());
        }
    }
}
