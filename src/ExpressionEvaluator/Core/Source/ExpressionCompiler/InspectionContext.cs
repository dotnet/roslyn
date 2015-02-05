// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal abstract class InspectionContext
    {
        internal abstract string GetExceptionTypeName();
        internal abstract string GetStowedExceptionTypeName();
        internal abstract string GetReturnValueTypeName(int index);
        internal abstract string GetObjectTypeNameById(string id);
    }
}
