// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class CSharpInScopeHoistedLocals : InScopeHoistedLocals
    {
        private readonly ImmutableSortedSet<int> _indices;

        public CSharpInScopeHoistedLocals(ImmutableSortedSet<int> indices)
        {
            _indices = indices;
        }

        public override bool IsInScope(string fieldName)
        {
            int index;
            if (GeneratedNames.TryParseSlotIndex(fieldName, out index))
            {
                return _indices.Contains(index);
            }
            else
            {
                Debug.Assert(false, $"Expected hoisted local field name, found '{fieldName}'");
                return true;
            }
        }
    }
}
