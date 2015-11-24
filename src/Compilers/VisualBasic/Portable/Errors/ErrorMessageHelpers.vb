' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend Module ErrorMessageHelpers
        <Extension()>
        Public Function ToDisplay(access As Accessibility) As String
            Select Case access
                Case Accessibility.NotApplicable
                    Return ""
                Case Accessibility.Private
                    Return "Private"
                Case Accessibility.Protected
                    Return "Protected"
                Case Accessibility.ProtectedOrFriend
                    Return "Protected Friend"
                Case Accessibility.ProtectedAndFriend
                    Return "Private Protected"
                Case Accessibility.Friend
                    Return "Friend"
                Case Accessibility.Public
                    Return "Public"
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(access)
            End Select
        End Function

    End Module
End Namespace
