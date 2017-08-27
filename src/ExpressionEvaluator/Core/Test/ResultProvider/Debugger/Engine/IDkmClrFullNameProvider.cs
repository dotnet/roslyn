// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll
#endregion

using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace Microsoft.VisualStudio.Debugger.ComponentInterfaces
{
    public interface IDkmClrFullNameProvider
    {
        string GetClrTypeName(
            DkmInspectionContext inspectionContext,
            DkmClrType clrType,
            DkmClrCustomTypeInfo customTypeInfo);

        string GetClrArrayIndexExpression(
            DkmInspectionContext inspectionContext,
            string[] indices);

        string GetClrCastExpression(
            DkmInspectionContext inspectionContext,
            string argument,
            DkmClrType clrType,
            DkmClrCustomTypeInfo customTypeInfo,
            DkmClrCastExpressionOptions castExpressionOptions);

        string GetClrObjectCreationExpression(
            DkmInspectionContext inspectionContext,
            DkmClrType clrType,
            DkmClrCustomTypeInfo customTypeInfo,
            string[] arguments);

        string GetClrValidIdentifier(
            DkmInspectionContext inspectionContext,
            string identifier);

        string GetClrMemberName(
            DkmInspectionContext inspectionContext,
            string parentFullName,
            DkmClrType clrType,
            DkmClrCustomTypeInfo customTypeInfo,
            string memberName,
            bool requiresExplicitCast,
            bool isStatic);

        bool ClrExpressionMayRequireParentheses(
            DkmInspectionContext inspectionContext,
            string expression);

        string GetClrExpressionAndFormatSpecifiers(
            DkmInspectionContext inspectionContext,
            string expression,
            out ReadOnlyCollection<string> formatSpecifiers);

        string GetClrExpressionForNull(DkmInspectionContext inspectionContext);

        string GetClrExpressionForThis(DkmInspectionContext inspectionContext);
    }
}
