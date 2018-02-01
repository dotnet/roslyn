// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;

namespace Roslyn.Test.Utilities
{
    public class NoIOperationValidationFactAttribute : FactAttribute
    {
        public NoIOperationValidationFactAttribute()
        {
#if TEST_IOPERATION_INTERFACE
            Skip = "TEST_IOPERATION_INTERFACE is set";
#endif 
        }
    }
}
