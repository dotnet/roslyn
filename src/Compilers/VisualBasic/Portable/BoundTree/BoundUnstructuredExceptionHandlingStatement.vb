' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend Partial Class BoundUnstructuredExceptionHandlingStatement

#If DEBUG Then
        Private Sub Validate()
            Debug.Assert(ContainsOnError OrElse ContainsResume OrElse TrackLineNumber)
            Debug.Assert(ResumeWithoutLabelOpt Is Nothing OrElse ContainsResume)

            If ResumeWithoutLabelOpt IsNot Nothing Then
                Debug.Assert(ResumeWithoutLabelOpt.Kind = SyntaxKind.OnErrorResumeNextStatement OrElse
                             ResumeWithoutLabelOpt.Kind = SyntaxKind.ResumeNextStatement OrElse
                             ResumeWithoutLabelOpt.Kind = SyntaxKind.ResumeStatement)
            End If
        End Sub
#End If

    End Class

End Namespace
