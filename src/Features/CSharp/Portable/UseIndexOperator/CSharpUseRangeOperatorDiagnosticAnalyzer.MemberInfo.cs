// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOperator
{
    internal partial class CSharpUseRangeOperatorDiagnosticAnalyzer
    {
        public struct MemberInfo
        {
            /// <summary>
            /// The Length/Count property on the type.  Must be public, non-static, no-parameter,
            /// int32 returning.
            /// </summary>
            public readonly IPropertySymbol LengthLikeProperty;

            /// <summary>
            /// Optional paired Slice overload that takes a Range parameter instead.
            /// </summary>
            public readonly IMethodSymbol SliceRangeMethodOpt;

            public MemberInfo(
                IPropertySymbol lengthLikeProperty,
                IMethodSymbol sliceRangeMethodOpt)
            {
                LengthLikeProperty = lengthLikeProperty;
                SliceRangeMethodOpt = sliceRangeMethodOpt;
            }
        } 
    }
}
