// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class DefaultInspectionContext : InspectionContext
    {
        internal static readonly InspectionContext Instance = new DefaultInspectionContext();

        private static readonly string s_objectType = typeof(object).AssemblyQualifiedName;
        private static readonly string s_exceptionType = typeof(Exception).AssemblyQualifiedName;

        internal override string GetExceptionTypeName()
        {
            return s_exceptionType;
        }

        internal override string GetStowedExceptionTypeName()
        {
            return s_exceptionType;
        }

        internal override string GetObjectTypeNameById(string id)
        {
            return char.IsNumber(id[0]) ? s_objectType : null;
        }

        internal override string GetReturnValueTypeName(int index)
        {
            return s_objectType;
        }
    }
}
