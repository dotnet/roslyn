' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.Serialization
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    <Serializable()>
    Friend NotInheritable Class VBDiagnostic
        Inherits DiagnosticWithInfo

        Friend Sub New(info As DiagnosticInfo, location As Location)
            MyBase.New(info, location)
        End Sub

        Private Sub New(info As SerializationInfo, context As StreamingContext)
            MyBase.New(info, context)
        End Sub

        Protected Overrides Sub GetObjectData(info As SerializationInfo, context As StreamingContext)
            MyBase.GetObjectData(info, context)
        End Sub

        Public Overrides Function ToString() As String
            Return VisualBasicDiagnosticFormatter.Instance.Format(Me)
        End Function

        Friend Overrides Function WithLocation(location As Location) As Diagnostic
            If location Is Nothing Then
                Throw New ArgumentNullException("location")
            End If

            If location IsNot Me.Location Then
                Return New VBDiagnostic(Me.Info, location)
            End If

            Return Me
        End Function

        Friend Overrides Function WithWarningAsError(isWarningAsError As Boolean) As Diagnostic
            If Me.IsWarningAsError <> isWarningAsError Then
                Return New VBDiagnostic(Me.Info.GetInstanceWithReportWarning(isWarningAsError), Me.Location)
            End If

            Return Me
        End Function

        Friend Overrides Function WithSeverity(severity As DiagnosticSeverity) As Diagnostic
            If Me.Severity <> severity Then
                Return New VBDiagnostic(Me.Info.GetInstanceWithSeverity(severity), Me.Location)
            End If

            Return Me
        End Function
    End Class
End Namespace

