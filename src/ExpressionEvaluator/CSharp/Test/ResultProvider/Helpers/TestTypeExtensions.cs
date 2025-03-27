// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
        public static string GetTypeName(this System.Type type, DkmClrCustomTypeInfo typeInfo = null, bool escapeKeywordIdentifiers = false, DkmInspectionContext inspectionContext = null)
        {
            var formatter = new CSharpFormatter();
            var clrType = new DkmClrType((TypeImpl)type);
            if (inspectionContext == null)
            {
                var inspectionSession = new DkmInspectionSession(ImmutableArray.Create<IDkmClrFormatter>(formatter), ImmutableArray.Create<IDkmClrResultProvider>(new CSharpResultProvider()));
                inspectionContext = new DkmInspectionContext(inspectionSession, DkmEvaluationFlags.None, radix: 10, runtimeInstance: null);
            }
            return escapeKeywordIdentifiers
                ? ((IDkmClrFullNameProvider)formatter).GetClrTypeName(inspectionContext, clrType, typeInfo)
                : inspectionContext.GetTypeName(clrType, typeInfo, Formatter.NoFormatSpecifiers);
        }
    }
}
