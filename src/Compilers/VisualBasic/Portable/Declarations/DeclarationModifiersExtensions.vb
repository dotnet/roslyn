' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    Friend Module DeclarationModifiersExtensions
        <Extension()>
        Friend Function ToAccessibility(modifiers As DeclarationModifiers) As Accessibility
            Select Case modifiers And DeclarationModifiers.AllAccessibilityModifiers
                Case DeclarationModifiers.Private : Return Accessibility.Private
                Case DeclarationModifiers.Public : Return Accessibility.Public
                Case DeclarationModifiers.Protected : Return Accessibility.Protected
                Case DeclarationModifiers.Friend : Return Accessibility.Friend
                Case DeclarationModifiers.Friend Or DeclarationModifiers.Protected : Return Accessibility.ProtectedOrFriend
                Case DeclarationModifiers.Private Or DeclarationModifiers.Protected : Return Accessibility.ProtectedAndFriend
                Case Else
                    ' this method shouldn't be used if modifiers contain conflicting accessibility flags
                    Throw ExceptionUtilities.UnexpectedValue(modifiers)
            End Select
        End Function
    End Module
End Namespace
