// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal static class TestTypeExtensions
    {
        public static string GetTypeName(this System.Type type, bool[] dynamicFlags = null, bool escapeKeywordIdentifiers = false)
        {
            return type.GetTypeName(DynamicFlagsCustomTypeInfo.Create(dynamicFlags).GetCustomTypeInfo(), escapeKeywordIdentifiers);
        }

        public static string GetTypeName(this System.Type type, DkmClrCustomTypeInfo typeInfo, bool escapeKeywordIdentifiers = false)
        {
            bool sawInvalidIdentifier;
            var result = CSharpFormatter.Instance.GetTypeName(new TypeAndCustomInfo((TypeImpl)type, typeInfo), escapeKeywordIdentifiers, out sawInvalidIdentifier);
            Assert.False(sawInvalidIdentifier);
            return result;
        }
    }
}
