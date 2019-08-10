' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
