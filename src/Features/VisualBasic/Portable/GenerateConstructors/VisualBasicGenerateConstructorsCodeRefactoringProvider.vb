' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.GenerateConstructors
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.PickMembers

Namespace Microsoft.CodeAnalysis.VisualBasic.GenerateConstructors
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.GenerateConstructorFromMembers), [Shared]>
    <ExtensionOrder(Before:=PredefinedCodeRefactoringProviderNames.GenerateEqualsAndGetHashCodeFromMembers)>
    Friend NotInheritable Class VisualBasicGenerateConstructorsCodeRefactoringProvider
        Inherits AbstractGenerateConstructorsCodeRefactoringProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        ''' <summary>
        ''' For testing purposes only.
        ''' </summary>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0034:Exported parts should have [ImportingConstructor]", Justification:="Used incorrectly by tests")>
        Friend Sub New(pickMembersService_forTesting As IPickMembersService)
            MyBase.New(pickMembersService_forTesting)
        End Sub

        Protected Overrides Function ContainingTypesOrSelfHasUnsafeKeyword(containingType As INamedTypeSymbol) As Boolean
            Return False
        End Function

        Protected Overrides Function ToDisplayString(parameter As IParameterSymbol, format As SymbolDisplayFormat) As String
            Return SymbolDisplay.ToDisplayString(parameter, format)
        End Function

        Protected Overrides Function PrefersThrowExpressionAsync(document As Document, cancellationToken As CancellationToken) As ValueTask(Of Boolean)
            ' No throw expression preference option is defined for VB because it doesn't support throw expressions.
            Return RoslynValueTaskExtensions.FromResult(False)
        End Function

        Protected Overrides Function TryMapToWritableInstanceField([property] As IPropertySymbol, cancellationToken As CancellationToken) As IFieldSymbol
            Return Nothing
        End Function
    End Class
End Namespace
