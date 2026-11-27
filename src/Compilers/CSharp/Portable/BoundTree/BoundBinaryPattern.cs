// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundBinaryPattern
    {
        private partial void Validate()
        {
            Debug.Assert(Left.InputType.Equals(InputType, TypeCompareKind.AllIgnoreOptions));
            Debug.Assert(Left is not BoundPatternWithUnionMatching);
            Debug.Assert(Right is not BoundPatternWithUnionMatching);

            if (Disjunction)
            {
                Debug.Assert(Right.InputType.Equals(InputType, TypeCompareKind.AllIgnoreOptions));
                // Is it worth asserting that NarrowedType is either the InputType, or or the NarrowedType
                // of one of the leaves in the Disjunction hierarchy?
            }
            else
            {
                Debug.Assert(Right.InputType.Equals(Left.NarrowedType, TypeCompareKind.AllIgnoreOptions));
                Debug.Assert(NarrowedType.Equals(Right.NarrowedType, TypeCompareKind.AllIgnoreOptions));
            }
        }
    }
}
