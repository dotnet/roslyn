' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend NotInheritable Class VBDiagnostic
        Inherits DiagnosticWithInfo

        Friend Sub New(info As DiagnosticInfo, location As Location, Optional isSuppressed As Boolean = False)
            MyBase.New(info, location, isSuppressed)
        End Sub

        Public Overrides Function ToString() As String
            Return VisualBasicDiagnosticFormatter.Instance.Format(Me)
        End Function

        Friend Overrides Function WithLocation(location As Location) As Diagnostic
            If location Is Nothing Then
                Throw New ArgumentNullException(NameOf(location))
            End If

            If location IsNot Me.Location Then
                Return New VBDiagnostic(Me.Info, location, Me.IsSuppressed)
            End If

            Return Me
        End Function

        Friend Overrides Function WithSeverity(severity As DiagnosticSeverity) As Diagnostic
            If Me.Severity <> severity Then
                Return New VBDiagnostic(Me.Info.GetInstanceWithSeverity(severity), Me.Location, Me.IsSuppressed)
            End If

            Return Me
        End Function

        Friend Overrides Function WithIsSuppressed(isSuppressed As Boolean) As Diagnostic
            If Me.IsSuppressed <> isSuppressed Then
                Return New VBDiagnostic(Me.Info, Me.Location, isSuppressed)
            End If

            Return Me
        End Function
    End Class
End Namespace

