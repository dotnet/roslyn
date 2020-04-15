' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

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
