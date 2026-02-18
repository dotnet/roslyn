// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundPatternWithUnionMatching
    {
        private partial void Validate()
        {
            Debug.Assert(UnionType is NamedTypeSymbol { IsUnionType: true });
            Debug.Assert(InputType.Equals(LeftOfPendingConjunction?.InputType ?? UnionType, TypeCompareKind.AllIgnoreOptions));
            Debug.Assert(UnionType == (object)(LeftOfPendingConjunction?.NarrowedType ?? InputType));
            Debug.Assert(NarrowedType == (object)ValuePattern.NarrowedType);
            Debug.Assert(ValuePattern is not BoundPatternWithUnionMatching);
        }

        public BoundPatternWithUnionMatching(SyntaxNode syntax, TypeSymbol unionType, BoundPropertySubpatternMember valueProperty, BoundPattern pattern, TypeSymbol inputType)
            : this(syntax, unionType, leftOfPendingConjunction: null, valueProperty, pattern, inputType, pattern.NarrowedType)
        {
        }

        public BoundPatternWithUnionMatching(SyntaxNode syntax, TypeSymbol unionType, BoundPattern? leftOfPendingConjunction, BoundPropertySubpatternMember valueProperty, BoundPattern pattern, TypeSymbol inputType)
            : this(syntax, unionType, leftOfPendingConjunction, valueProperty, pattern, inputType, pattern.NarrowedType)
        {
        }
    }
}
