' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
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
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0034:Exported parts should have [ImportingConstructor]", Justification:="Used incorrectly by tests")>
        Friend Sub New(pickMembersService_forTesting As IPickMembersService)
            MyBase.New(pickMembersService_forTesting)
        End Sub

        Protected Overrides Function PrefersThrowExpression(options As DocumentOptionSet) As Boolean
            ' No throw expression preference option is defined for VB because it doesn't support throw expressions.
            Return False
        End Function
    End Class
End Namespace
