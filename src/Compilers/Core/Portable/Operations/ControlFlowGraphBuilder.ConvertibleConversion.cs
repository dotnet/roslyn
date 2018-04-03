// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Operations
{
    internal partial class ControlFlowGraphBuilder
    {
        private class ConvertibleConversion : IConvertibleConversion
        {
            public static readonly ConvertibleConversion Instance = new ConvertibleConversion();

            public CommonConversion ToCommonConversion()
            {
                return new CommonConversion(exists: true); 
            }
        }
    }
}
