// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.Cci;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CodeGen
{
    internal sealed class FullLocalSlotManager : LocalSlotManager
    {
        // all locals in order
        private ImmutableArray<ILocalDefinition>.Builder allLocals;

        public override ImmutableArray<ILocalDefinition> LocalsInOrder()
        {
            if (allLocals == null)
            {
                return ImmutableArray<ILocalDefinition>.Empty;
            }
            else
            {
                return this.allLocals.ToImmutable();
            }
        }

        protected override LocalDefinition DeclareLocalInternal(
            ITypeReference type,
            object identity,
            string name,
            bool isCompilerGenerated,
            LocalSlotConstraints constraints,
            bool isDynamic,
            ImmutableArray<TypedConstant> dynamicTransformFlags)
        {
            if (allLocals == null)
            {
                allLocals = ImmutableArray.CreateBuilder<ILocalDefinition>(1);
            }

            var local = new LocalDefinition(
                identity: identity,
                name: name,
                type: type,
                slot: this.allLocals.Count,
                isCompilerGenerated: isCompilerGenerated,
                constraints: constraints,
                isDynamic: isDynamic,
                dynamicTransformFlags: dynamicTransformFlags);

            this.allLocals.Add(local);
            return local;
        }
    }
}
