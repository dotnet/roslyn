' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Implementation.GoToDefinition
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.GoToDefinition
    <ExportLanguageService(GetType(IGoToDefinitionService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicGoToDefinitionService
        Inherits AbstractGoToDefinitionService

        <ImportingConstructor>
        Public Sub New(<ImportMany> presenters As IEnumerable(Of Lazy(Of INavigableItemsPresenter)))
            MyBase.New(presenters)
        End Sub

        Protected Overrides Function FindRelatedExplicitlyDeclaredSymbol(symbol As ISymbol, compilation As Compilation) As ISymbol
            ' For example: My.Forms.[|LoginForm|]
            ' LoginForm is a SynthesizedMyGroupCollectionPropertySymbol with no Location. Use the
            ' type of this property, the actual LoginForm type itself, for navigation purposes.

            If symbol.IsKind(SymbolKind.Property) AndAlso symbol.IsImplicitlyDeclared Then
                Dim propertySymbol = DirectCast(symbol, IPropertySymbol)
                If propertySymbol.ContainingType IsNot Nothing AndAlso
                   propertySymbol.ContainingType.Name = "MyForms" AndAlso
                   propertySymbol.ContainingType.ContainingNamespace IsNot Nothing AndAlso
                   propertySymbol.ContainingType.ContainingNamespace.IsMyNamespace(compilation) Then

                    Return propertySymbol.Type
                End If
            End If

            Return symbol
        End Function
    End Class
End Namespace
