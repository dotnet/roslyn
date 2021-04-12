' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.ExpressionEvaluator
Imports Microsoft.VisualStudio.Debugger.Clr
Imports Microsoft.VisualStudio.Debugger.ComponentInterfaces
Imports Microsoft.VisualStudio.Debugger.Evaluation
Imports Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation

Namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator.UnitTests

    Friend Module TestTypeExtensions

        <Extension>
        Public Function GetTypeName(type As System.Type, Optional typeInfo As DkmClrCustomTypeInfo = Nothing, Optional escapeKeywordIdentifiers As Boolean = False, Optional inspectionContext As DkmInspectionContext = Nothing) As String
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
