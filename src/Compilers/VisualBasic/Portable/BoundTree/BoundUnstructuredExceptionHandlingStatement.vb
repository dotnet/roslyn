' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Friend Class BoundUnstructuredExceptionHandlingStatement

#If DEBUG Then
        Private Sub Validate()
            Debug.Assert(Me.ContainsOnError OrElse Me.ContainsResume OrElse Me.TrackLineNumber)
            Debug.Assert(Me.ResumeWithoutLabelOpt Is Nothing OrElse Me.ContainsResume)

            If Me.ResumeWithoutLabelOpt IsNot Nothing Then
                Debug.Assert(Me.ResumeWithoutLabelOpt.Kind = SyntaxKind.OnErrorResumeNextStatement OrElse
                             Me.ResumeWithoutLabelOpt.Kind = SyntaxKind.ResumeNextStatement OrElse
                             Me.ResumeWithoutLabelOpt.Kind = SyntaxKind.ResumeStatement)
            End If
        End Sub
#End If

    End Class

End Namespace
