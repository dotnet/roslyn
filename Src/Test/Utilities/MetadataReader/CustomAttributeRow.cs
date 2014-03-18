// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection.Metadata;
using Roslyn.Utilities;

namespace Roslyn.Test.Utilities
{
    internal struct CustomAttributeRow : IEquatable<CustomAttributeRow>
    {
        public readonly Handle ParentToken;
        public readonly Handle ConstructorToken;

        public CustomAttributeRow(Handle parentToken, Handle constructorToken)
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
