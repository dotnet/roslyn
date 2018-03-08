' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.GenerateEqualsAndGetHashCodeFromMembers
Imports Microsoft.CodeAnalysis.PickMembers
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.CodeGeneration

Namespace Microsoft.CodeAnalysis.VisualBasic.GenerateEqualsAndGetHashCodeFromMembers
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic,
        Name:=PredefinedCodeRefactoringProviderNames.GenerateEqualsAndGetHashCodeFromMembers), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeRefactoringProviderNames.GenerateConstructorFromMembers,
                    Before:=PredefinedCodeRefactoringProviderNames.AddConstructorParametersFromMembers)>
    Friend Class VisualBasicGenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider
        Inherits AbstractGenerateEqualsAndGetHashCodeFromMembersCodeRefactoringProvider

        Public Sub New()
            Me.New(Nothing)
        End Sub

        Public Sub New(pickMembersService As IPickMembersService)
            MyBase.New(pickMembersService)
        End Sub

        Protected Overrides Function WrapWithUnchecked(statements As ImmutableArray(Of SyntaxNode)) As ImmutableArray(Of SyntaxNode)
            Return statements
        End Function
    End Class
End Namespace
