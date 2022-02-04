' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.SpellCheck
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SpellCheck

    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.SpellCheck), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeFixProviderNames.RemoveUnnecessaryCast)>
    Partial Friend Class VisualBasicSpellCheckCodeFixProvider
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

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New()
        End Sub

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(BC30002, IDEDiagnosticIds.UnboundIdentifierId, BC30451, BC30456, BC32045)
            End Get
        End Property

        Protected Overrides Function ShouldSpellCheck(name As SimpleNameSyntax) As Boolean
            Return True
        End Function

        Protected Overrides Function DescendIntoChildren(arg As SyntaxNode) As Boolean
            Return TypeOf arg IsNot TypeArgumentListSyntax
        End Function

        Protected Overrides Function IsGeneric(nameToken As SyntaxToken) As Boolean
            Return nameToken.GetNextToken().Kind() = SyntaxKind.OpenParenToken AndAlso
                nameToken.GetNextToken().GetNextToken().Kind() = SyntaxKind.OfKeyword
        End Function

        Protected Overrides Function IsGeneric(nameNode As SimpleNameSyntax) As Boolean
            Return nameNode.Kind() = SyntaxKind.GenericName
        End Function

        Protected Overrides Function IsGeneric(completionItem As CompletionItem) As Boolean
            Return completionItem.DisplayTextSuffix.StartsWith("(Of")
        End Function

        Protected Overrides Function CreateIdentifier(nameToken As SyntaxToken, newName As String) As SyntaxToken
            Dim index = newName.IndexOf("(Of")
            newName = If(index < 0, newName, newName.Substring(0, index))
            If nameToken.IsBracketed() AndAlso Not newName.StartsWith("[") Then
                newName = "[" + newName + "]"
            End If

            Return SyntaxFactory.Identifier(newName).WithTriviaFrom(nameToken)
        End Function
    End Class
End Namespace
