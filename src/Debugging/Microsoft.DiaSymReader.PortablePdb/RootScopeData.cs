// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
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
        internal override int StartOffset => 0;

        internal override int EndOffset
        {
            get
            {
                var mdReader = SymMethod.MetadataReader;
                var allScopes = mdReader.GetLocalScopes(SymMethod.DebugHandle);

                foreach (var handle in allScopes)
                {
                    // the first scope spans the entire body
                    return AdjustEndOffset(mdReader.GetLocalScope(handle).EndOffset);
                }

                // method has no body
                return 0;
            }
        }

        protected override ImmutableArray<ChildScopeData> CreateChildren()
        {
            foreach (var handle in SymMethod.MetadataReader.GetLocalScopes(SymMethod.DebugHandle))
            {
                // The root scope has only a single child scope, 
                // which is the first scope in the scopes belonging to the method:
                return ImmutableArray.Create(new ChildScopeData(SymMethod, this, handle));
            }

            // method has no body
            return ImmutableArray<ChildScopeData>.Empty;
        }

        internal override int GetConstants(int bufferLength, out int count, ISymUnmanagedConstant[] constants)
        {
            // C# and VB never define any constants in the root scope 
            count = 0;
            return HResult.S_OK;
        }

        internal override int GetLocals(int bufferLength, out int count, ISymUnmanagedVariable[] locals)
        {
            // C# and VB never define any locals in the root scope 
            count = 0;
            return HResult.S_OK;
        }
    }
}