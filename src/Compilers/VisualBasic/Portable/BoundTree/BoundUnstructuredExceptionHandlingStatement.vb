' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundUnstructuredExceptionHandlingStatement

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
