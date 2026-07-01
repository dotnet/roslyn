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
            Debug.Assert(UnionMatchingInputType.IsSubjectForUnionMatching);
            Debug.Assert(InputType.Equals(LeftOfPendingConjunction?.InputType ?? UnionMatchingInputType, TypeCompareKind.AllIgnoreOptions));
            Debug.Assert(UnionMatchingInputType == (object)(LeftOfPendingConjunction?.NarrowedType ?? InputType));
            Debug.Assert(NarrowedType == (object)(SharedRightOfPendingConjunction ?? ExclusiveValuePattern).NarrowedType);
            Debug.Assert(ExclusiveValuePattern is not BoundPatternWithUnionMatching);
            Debug.Assert(SharedRightOfPendingConjunction is not BoundPatternWithUnionMatching);

            if (ExclusiveInstancePattern is not null)
            {
                Debug.Assert(SharedRightOfPendingConjunction is not null || NarrowedType.Equals(ExclusiveInstancePattern.NarrowedType, TypeCompareKind.ConsiderEverything));
                Debug.Assert(ExclusiveInstancePattern.InputType == (object)UnionMatchingInputType);
            }
        }

        public BoundPatternWithUnionMatching(SyntaxNode syntax, TypeSymbol unionType, BoundTypePattern? exclusiveInstancePattern, BoundPropertySubpatternMember valueProperty, BoundPattern exclusiveValuePattern, BoundPattern? sharedRightOfPendingConjunction, TypeSymbol inputType)
            : this(syntax, unionType, leftOfPendingConjunction: null,
                   exclusiveInstancePattern: exclusiveInstancePattern,
                   valueProperty: valueProperty,
                   exclusiveValuePattern: exclusiveValuePattern,
                   sharedRightOfPendingConjunction: sharedRightOfPendingConjunction,
                   inputType: inputType,
                   narrowedType: (sharedRightOfPendingConjunction ?? exclusiveValuePattern).NarrowedType)
        {
        }

        public BoundPatternWithUnionMatching(SyntaxNode syntax, TypeSymbol unionType, BoundPattern? leftOfPendingConjunction, BoundTypePattern? exclusiveInstancePattern, BoundPropertySubpatternMember valueProperty, BoundPattern exclusiveValuePattern, BoundPattern? sharedRightOfPendingConjunction, TypeSymbol inputType)
            : this(syntax, unionType, leftOfPendingConjunction: leftOfPendingConjunction,
                   exclusiveInstancePattern: exclusiveInstancePattern,
                   valueProperty: valueProperty,
                   exclusiveValuePattern: exclusiveValuePattern,
                   sharedRightOfPendingConjunction: sharedRightOfPendingConjunction,
                   inputType: inputType,
                   narrowedType: (sharedRightOfPendingConjunction ?? exclusiveValuePattern).NarrowedType)
        {
        }
    }
}
