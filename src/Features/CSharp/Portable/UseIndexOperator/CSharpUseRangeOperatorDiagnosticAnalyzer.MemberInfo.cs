// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.CSharp.UseIndexOperator
{
    internal partial class CSharpUseRangeOperatorDiagnosticAnalyzer
    {
        private struct MemberInfo
        {
            public readonly IPropertySymbol LengthOrCountProperty;
            public readonly IMethodSymbol SliceLikeMethod;

            public MemberInfo(IPropertySymbol lengthOrCountPropertyOpt, IMethodSymbol sliceLikeMethod)
            {
                LengthOrCountProperty = lengthOrCountPropertyOpt;
                SliceLikeMethod = sliceLikeMethod;
            }
        } 
    }
}
