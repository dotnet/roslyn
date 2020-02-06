' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.GenerateConstructorFromMembers
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PickMembers

Namespace Microsoft.CodeAnalysis.VisualBasic.GenerateConstructorFromMembers
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.GenerateConstructorFromMembers), [Shared]>
    <ExtensionOrder(Before:=PredefinedCodeRefactoringProviderNames.GenerateEqualsAndGetHashCodeFromMembers)>
    Friend NotInheritable Class VisualBasicGenerateConstructorFromMembersCodeRefactoringProvider
        Inherits AbstractGenerateConstructorFromMembersCodeRefactoringProvider

        <ImportingConstructor>
        Public Sub New()
        End Sub

        ''' <summary>
        ''' For testing purposes only.
        ''' </summary>
        Friend Sub New(pickMembersService_forTesting As IPickMembersService)
            MyBase.New(pickMembersService_forTesting)
        End Sub

        Protected Overrides Function PrefersThrowExpression(options As DocumentOptionSet) As Boolean
            ' No throw expression preference option is defined for VB because it doesn't support throw expressions.
            Return False
        End Function
    End Class
End Namespace
