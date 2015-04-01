// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;

namespace Microsoft.DiaSymReader.PortablePdb
{
    internal sealed class RootScopeData : ScopeData
    {
        internal RootScopeData(SymMethod symMethod)
            : base(symMethod)
        {
        }

        internal override ScopeData Parent => null;

        protected override ImmutableArray<ChildScopeData> CreateChildren()
        {
            var allScopes = SymMethod.MetadataReader.GetLocalScopes(SymMethod.Handle);

            // TODO: pool?
            var builder = ImmutableArray.CreateBuilder<ChildScopeData>(allScopes.Count);

            foreach (var childHandle in allScopes)
            {
                builder.Add(new ChildScopeData(SymMethod, this, childHandle));
            }

            return builder.ToImmutable();
        }

        internal override int GetConstants(int bufferLength, out int count, ISymUnmanagedConstant[] constants)
        {
            throw new NotImplementedException();
        }

        internal override int GetLocals(int bufferLength, out int count, ISymUnmanagedVariable[] locals)
        {
            throw new NotImplementedException();
        }
    }
}
