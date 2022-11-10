// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Reflection.Metadata;
using Roslyn.Utilities;

namespace Roslyn.Test.Utilities
{
    internal readonly struct CustomAttributeRow : IEquatable<CustomAttributeRow>
    {
        public readonly EntityHandle ParentToken;
        public readonly EntityHandle ConstructorToken;

        public CustomAttributeRow(EntityHandle parentToken, EntityHandle constructorToken)
        {
            this.ParentToken = parentToken;
            this.ConstructorToken = constructorToken;
        }

        public bool Equals(CustomAttributeRow other)
        {
            return this.ParentToken == other.ParentToken
                && this.ConstructorToken == other.ConstructorToken;
        }

        public override bool Equals(object obj)
        {
            return base.Equals((CustomAttributeRow)obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(ParentToken.GetHashCode(), ConstructorToken.GetHashCode());
        }
    }
}
