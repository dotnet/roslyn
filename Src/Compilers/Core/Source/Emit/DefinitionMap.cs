// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Cci;
using Microsoft.CodeAnalysis.CodeGen;
using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis.Emit
{
    internal abstract class DefinitionMap
    {
        internal static readonly GetPreviousLocalSlot NoPreviousLocalSlot = (identity, type, constraints) => -1;

        internal abstract bool TryGetTypeHandle(ITypeDefinition def, out TypeHandle handle);
        internal abstract bool TryGetEventHandle(IEventDefinition def, out EventHandle handle);
        internal abstract bool TryGetFieldHandle(IFieldDefinition def, out FieldHandle handle);
        internal abstract bool TryGetMethodHandle(IMethodDefinition def, out MethodHandle handle);
        internal abstract bool TryGetPropertyHandle(IPropertyDefinition def, out PropertyHandle handle);

        internal abstract bool TryGetPreviousLocals(
            EmitBaseline baseline,
            IMethodSymbol method,
            out ImmutableArray<EncLocalInfo> previousLocals,
            out GetPreviousLocalSlot getPreviousLocalSlot);

        internal abstract ImmutableArray<EncLocalInfo> GetLocalInfo(
            IMethodDefinition def,
            ImmutableArray<LocalDefinition> localDefs);

        internal abstract bool DefinitionExists(IDefinition def);
    }
}
