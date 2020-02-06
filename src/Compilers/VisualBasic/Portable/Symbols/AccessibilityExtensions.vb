' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Generic
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Runtime.CompilerServices
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend Module AccessibilityExtensions
        <Extension()>
        Friend Function ToDiagnosticString(a As Accessibility) As String
            Select Case a
                Case Accessibility.Public
                    Return "Public"
                Case Accessibility.Friend
                    Return "Friend"
                Case Accessibility.Private
                    Return "Private"
                Case Accessibility.Protected
                    Return "Protected"
                Case Accessibility.ProtectedOrFriend
                    Return "Protected Friend"
                Case Accessibility.ProtectedAndFriend
                    Return "Private Protected"
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(a)
            End Select
        End Function
    End Module
End Namespace
