// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection.Metadata;
using Roslyn.Utilities;

namespace Roslyn.Test.Utilities
{
    internal struct CustomAttributeRow : IEquatable<CustomAttributeRow>
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
            return this is { ParentToken: other.ParentToken, ConstructorToken: other.ConstructorToken };
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
