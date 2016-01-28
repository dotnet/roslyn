' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.VisualStudio.Debugger.Clr
Imports Microsoft.VisualStudio.Debugger.ComponentInterfaces
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator

    Friend Module TestTypeExtensions

        <Extension>
        Public Function GetTypeName(type As System.Type, Optional dynamicFlags As Boolean() = Nothing, Optional escapeKeywordIdentifiers As Boolean = False) As String
            Return type.GetTypeName(DynamicFlagsCustomTypeInfo.Create(dynamicFlags).GetCustomTypeInfo(), escapeKeywordIdentifiers, Nothing)
        End Function

        <Extension>
        Public Function GetTypeName(type As System.Type, typeInfo As DkmClrCustomTypeInfo, Optional escapeKeywordIdentifiers As Boolean = False, Optional inspectionContext As DkmInspectionContext = Nothing) As String
            Dim formatter = New VisualBasicFormatter()
            Dim clrType = New DkmClrType(New TypeImpl(type))
            If inspectionContext Is Nothing Then
                Dim inspectionSession = New DkmInspectionSession(ImmutableArray.Create(Of IDkmClrFormatter)(New VisualBasicFormatter()), ImmutableArray.Create(Of IDkmClrResultProvider)(New VisualBasicResultProvider()))
                inspectionContext = New DkmInspectionContext(inspectionSession, DkmEvaluationFlags.None, radix:=10, runtimeInstance:=Nothing)
            End If
            Return If(escapeKeywordIdentifiers,
                DirectCast(formatter, IDkmClrFullNameProvider).GetClrTypeName(inspectionContext, clrType, typeInfo),
                DirectCast(formatter, IDkmClrFormatter).GetTypeName(inspectionContext, clrType, typeInfo, Microsoft.CodeAnalysis.ExpressionEvaluator.Formatter.NoFormatSpecifiers))
        End Function

    End Module

End Namespace
