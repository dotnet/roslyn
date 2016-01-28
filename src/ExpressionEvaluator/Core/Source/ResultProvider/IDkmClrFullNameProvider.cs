// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Debugger.Clr;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace Microsoft.VisualStudio.Debugger.ComponentInterfaces
{
    public interface IDkmClrFullNameProvider
    {
        /// <summary>
        /// Return the type name in a form valid in the language (e.g.: escaping keywords)
        /// or null if the name cannot be represented as valid syntax.
        /// </summary>
        string GetClrTypeName(DkmInspectionContext inspectionContext, DkmClrType type, DkmClrCustomTypeInfo customTypeInfo);

        /// <summary>
        /// Return an array index expression. Should not return null.
        /// </summary>
        string GetClrArrayIndexExpression(DkmInspectionContext inspectionContext, int[] indices);

        /// <summary>
        /// Return a cast expression or null if the type name would be invalid syntax.
        /// </summary>
        string GetClrCastExpression(DkmInspectionContext inspectionContext, string argument, DkmClrType type, DkmClrCustomTypeInfo customTypeInfo, bool parenthesizeArgument, bool parenthesizeEntireExpression);

        /// <summary>
        /// Return an object creation expression or null if the type name would be invalid syntax.
        /// </summary>
        string GetClrObjectCreationExpression(DkmInspectionContext inspectionContext, DkmClrType type, DkmClrCustomTypeInfo customTypeInfo, string arguments);

        /// <summary>
        /// Return the identifier in a form valid in the language (e.g.: escaping keywords)
        /// or null if the identifier cannot be represented as a valid identifier.
        /// </summary>
        string GetClrValidIdentifier(DkmInspectionContext inspectionContext, string identifier);

        /// <summary>
        /// Return a member access expression or null if the expression cannot be
        /// represented as valid syntax.
        /// </summary>
        string GetClrMemberName(
            DkmInspectionContext inspectionContext,
            string parentFullName,
            DkmClrType declaringType,
            DkmClrCustomTypeInfo declaringTypeInfo,
            string memberName,
            bool memberAccessRequiresExplicitCast,
            bool memberIsStatic);

        /// <summary>
        /// Return true if the expression may require parentheses when used
        /// as a sub-expression in the language.
        /// </summary>
        bool ClrExpressionMayRequireParentheses(DkmInspectionContext inspectionContext, string expression);

        /// <summary>
        /// Split the string into the expression and format specifier parts.
        /// Returns the expression without format specifiers.
        /// </summary>
        string GetClrExpressionAndFormatSpecifiers(DkmInspectionContext inspectionContext, string expression, out ReadOnlyCollection<string> formatSpecifiers);
    }
}
