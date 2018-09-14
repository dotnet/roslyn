// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Roslyn.Test.Utilities
{
    public class NoIOperationValidationFactAttribute : FactAttribute
    {
        public NoIOperationValidationFactAttribute()
        {
            if (CompilationExtensions.EnableVerifyIOperation)
            {
                Skip = "Test not run during IOperation verification";
            }
        }
    }
}
