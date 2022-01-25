// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    internal sealed class CustomModifiersTuple
    {
        private readonly ImmutableArray<CustomModifier> _typeCustomModifiers;
        private readonly ImmutableArray<CustomModifier> _refCustomModifiers;
        public static readonly CustomModifiersTuple Empty = new CustomModifiersTuple(ImmutableArray<CustomModifier>.Empty, ImmutableArray<CustomModifier>.Empty);

        private CustomModifiersTuple(ImmutableArray<CustomModifier> typeCustomModifiers, ImmutableArray<CustomModifier> refCustomModifiers)
        {
            _typeCustomModifiers = typeCustomModifiers.NullToEmpty();
            _refCustomModifiers = refCustomModifiers.NullToEmpty();
        }

        public static CustomModifiersTuple Create(ImmutableArray<CustomModifier> typeCustomModifiers, ImmutableArray<CustomModifier> refCustomModifiers)
        {
            if (typeCustomModifiers.IsDefaultOrEmpty && refCustomModifiers.IsDefaultOrEmpty)
            {
                return Empty;
            }

            return new CustomModifiersTuple(typeCustomModifiers, refCustomModifiers);
        }

        public ImmutableArray<CustomModifier> TypeCustomModifiers { get { return _typeCustomModifiers; } }
        public ImmutableArray<CustomModifier> RefCustomModifiers { get { return _refCustomModifiers; } }
    }
}
