' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Structure

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    <ExportLanguageServiceFactory(GetType(BlockStructureService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicBlockStructureServiceFactory
        Implements ILanguageServiceFactory

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Function CreateLanguageService(languageServices As HostLanguageServices) As ILanguageService Implements ILanguageServiceFactory.CreateLanguageService
            Return New VisualBasicBlockStructureService(languageServices.LanguageServices.SolutionServices)
        End Function
    End Class

    Friend Class VisualBasicBlockStructureService
        Inherits BlockStructureServiceWithProviders

        Friend Sub New(services As SolutionServices)
            MyBase.New(services)
        End Sub

        Public Overrides ReadOnly Property Language As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

        Protected Overrides Function GetBuiltInProviders() As ImmutableArray(Of BlockStructureProvider)
            Return ImmutableArray.Create(Of BlockStructureProvider)(New VisualBasicBlockStructureProvider())
        End Function
    End Class
End Namespace
