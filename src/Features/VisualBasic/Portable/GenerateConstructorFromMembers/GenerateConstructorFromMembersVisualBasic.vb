Imports System.Composition
Imports System.Diagnostics.Tracing
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.GenerateConstructorFromMembers
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PickMembers

Namespace Microsoft.CodeAnalysis.VisualBasic.GenerateConstructorFromMembers

    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.GenerateConstructorFromMembers), [Shared]>
    <ExtensionOrder(Before:=PredefinedCodeRefactoringProviderNames.GenerateEqualsAndGetHashCodeFromMembers)>
    Friend Class GenerateConstructorFromMembersVisualBasic
        Inherits AbstractGenerateConstructorFromMembersCodeRefactoringProvider


        <ImportingConstructor>
        Public Sub New()
            Me.New(Nothing)
        End Sub

        Public Sub New(pickMembersService_forTesting As IPickMembersService)
            MyBase.New(pickMembersService_forTesting)
        End Sub

        Protected Overrides Function GetNullCheckOptionEnabled(optionSet As DocumentOptionSet) As Boolean
            Return False
        End Function
    End Class
End Namespace
