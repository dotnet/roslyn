// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.Cci
{
    internal sealed class StaticConstructor(
        ITypeDefinition containingTypeDefinition, ushort maxStack, ImmutableArray<byte> il)
        : MethodDefinitionBase(containingTypeDefinition, maxStack, il)
    {
        public override string Name => WellKnownMemberNames.StaticConstructorName;
        public override TypeMemberVisibility Visibility => TypeMemberVisibility.Private;
        public override bool IsRuntimeSpecial => true;
        public override bool IsSpecialName => true;
    }
}
