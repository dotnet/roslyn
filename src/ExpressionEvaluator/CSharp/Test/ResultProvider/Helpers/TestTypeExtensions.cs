// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal static class TestTypeExtensions
    {
        public static string GetTypeName(this System.Type type, bool[] dynamicFlags = null, bool escapeKeywordIdentifiers = false)
        {
            return type.GetTypeName(DynamicFlagsCustomTypeInfo.Create(dynamicFlags).GetCustomTypeInfo(), escapeKeywordIdentifiers, null);
        }

        public static string GetTypeName(this System.Type type, DkmClrCustomTypeInfo typeInfo, bool escapeKeywordIdentifiers = false, DkmInspectionContext inspectionContext = null)
        {
            var formatter = new CSharpFormatter();
            var clrType = new DkmClrType((TypeImpl)type);
            if (inspectionContext == null)
            {
                var inspectionSession = new DkmInspectionSession(ImmutableArray.Create<IDkmClrFormatter>(formatter), ImmutableArray.Create<IDkmClrResultProvider>(new CSharpResultProvider()));
                inspectionContext = new DkmInspectionContext(inspectionSession, DkmEvaluationFlags.None, radix: 10, runtimeInstance: null);
            }
            return escapeKeywordIdentifiers ?
                ((IDkmClrFullNameProvider)formatter).GetClrTypeName(inspectionContext, clrType, typeInfo) :
                inspectionContext.GetTypeName(clrType, typeInfo, Formatter.NoFormatSpecifiers);
        }
    }
}
