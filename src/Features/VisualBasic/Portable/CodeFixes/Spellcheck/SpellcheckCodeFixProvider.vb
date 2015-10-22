' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.SpellCheck
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeFixes.Spellcheck

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.SpellCheck), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.RemoveUnnecessaryCast)>
    Partial Friend Class SpellCheckCodeFixProvider
        Inherits AbstractSpellCheckCodeFixProvider(Of SimpleNameSyntax)

        ''' <summary>
        ''' Type xxx is not defined
        ''' </summary>
        Friend Const BC30002 = "BC30002"

        ''' <summary>
        ''' Error 'x' is not declared
        ''' </summary>
        Friend Const BC30451 = "BC30451"

        ''' <summary>
        ''' xxx is not a member of yyy
        ''' </summary>
        Friend Const BC30456 = "BC30456"

        ''' <summary>
        ''' 'A' has no type parameters and so cannot have type arguments.
        ''' </summary>
        Friend Const BC32045 = "BC32045"

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(BC30002, BC30451, BC30456, BC32045)
            End Get
        End Property

        Protected Overrides Function IsGeneric(nameNode As SimpleNameSyntax) As Boolean
            Return nameNode.Kind() = SyntaxKind.GenericName
        End Function

        Protected Overrides Function IsGeneric(completionItem As CompletionItem) As Boolean
            Return completionItem.DisplayText.Contains("(Of")
        End Function

        Protected Overrides Function CreateIdentifier(nameNode As SimpleNameSyntax, newName As String) As SyntaxToken
            Dim index = newName.IndexOf("(Of")
            newName = If(index < 0, newName, newName.Substring(0, index))
            If nameNode.Identifier.IsBracketed() AndAlso Not newName.StartsWith("[") Then
                newName = "[" + newName + "]"
            End If
            Return SyntaxFactory.Identifier(newName).WithTriviaFrom(nameNode.Identifier)
        End Function
    End Class
End Namespace