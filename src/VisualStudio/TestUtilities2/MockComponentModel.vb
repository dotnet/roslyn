' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Composition
Imports System.ComponentModel.Composition.Primitives
Imports Microsoft.VisualStudio.ComponentModelHost
Imports Microsoft.VisualStudio.Composition

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests
    Friend NotInheritable Class MockComponentModel
        Implements IComponentModel

        Private ReadOnly _exportProvider As ExportProvider
        Private ReadOnly _providedServices As New Dictionary(Of Type, Object)

        Public Sub New(exportProvider As ExportProvider)
            _exportProvider = exportProvider
        End Sub

#Disable Warning BC40000 ' Type or member is obsolete
        Public ReadOnly Property DefaultCatalog As ComposablePartCatalog Implements IComponentModel.DefaultCatalog
            Get
                Throw New NotImplementedException
            End Get
        End Property
#Enable Warning BC40000 ' Type or member is obsolete

        Public ReadOnly Property DefaultCompositionService As ICompositionService Implements IComponentModel.DefaultCompositionService
            Get
                Throw New NotImplementedException
            End Get
        End Property

        Public ReadOnly Property DefaultExportProvider As Hosting.ExportProvider Implements IComponentModel.DefaultExportProvider
            Get
                Return _exportProvider.AsExportProvider()
            End Get
        End Property

        Public Function GetCatalog(catalogName As String) As ComposablePartCatalog Implements IComponentModel.GetCatalog
            Throw New NotImplementedException
        End Function

        Friend Sub ProvideService(Of T)(export As T)
            _providedServices.Add(GetType(T), export)
        End Sub

        Public Function GetExtensions(Of T As Class)() As IEnumerable(Of T) Implements IComponentModel.GetExtensions
            Return _exportProvider.GetExportedValues(Of T)()
        End Function

        Public Function GetService(Of T As Class)() As T Implements IComponentModel.GetService
            Dim possibleService As Object = Nothing
            If _providedServices.TryGetValue(GetType(T), possibleService) Then
                Return DirectCast(possibleService, T)
            End If

            Return _exportProvider.GetExportedValue(Of T)()
        End Function
    End Class
End Namespace
